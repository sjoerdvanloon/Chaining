using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;

namespace Chaining
{
    public enum Logger
    {
        Action, Page, PageAction
    }

    public enum Event
    {
        OnResolve
    }

    public interface IOnResult<T>
    {
        IDirectResult<T> Do(params Expression<Action<T>>[] actions);

        IOnResult<T> SetLogger(Logger logger, Action<string> log);
        IOnResult<T> SetEvent(Event e, Action action);
    }

    public interface IAndResult<T>
    {
        IDirectResult<T> Do(params Expression<Action<T>>[] actions);
    }

    public interface IDirectResult<T>
    {
        IDirectResult<T> Do(params Expression<Action<T>>[] actions);

        IAndResult<T> And { get; }

        IEndResult<T> WaitFor(int expected, Expression<Func<T, int>> action, double timeout = 1.0);
    }

    public interface IEndResult<T>
    {

    }

    public interface IComposite<T> : IAndResult<T>, IDirectResult<T>, IEndResult<T>, IOnResult<T> where T : IPage, new()
    {
    }

    public class FluentChainBuilder<T> : IComposite<T> where T : IPage, new()
    {
        private Action<string> _logPage = (x) => { Trace.WriteLine($"{x}"); };
        private Action<string> _logAction = (x) => { Trace.WriteLine($"{x}"); };
        private Action<string> _logPageAction = (x) => { Trace.WriteLine($"{x}"); };
        private Action _onResolveEvent = () => { };

        public T Page { get; set; }

        public static IOnResult<T> On()
        {
            return new FluentChainBuilder<T>();
        }

        public FluentChainBuilder()
        {
        }

        IAndResult<T> IDirectResult<T>.And => this;

        private T GetOrResolvePage()
        {
            if (Page == null)
            {
                _logPage(typeof(T).Name);
                Page = (T)Activator.CreateInstance(typeof(T));
                Page.Log = _logPageAction;

                if (_onResolveEvent != null)
                    _onResolveEvent.Invoke();
            }

            return Page;
        }

        IDirectResult<T> Do(params Expression<Action<T>>[] actions)
        {
            Page = GetOrResolvePage();

            foreach (var action in actions)
            {
                var methodCallExp = (MethodCallExpression)action.Body;
                string methodName = methodCallExp.Method.Name;
                _logAction(methodName);
                var x = action.Compile();
                x.Invoke(Page);
            }

            return this;
        }

        IEndResult<T> IDirectResult<T>.WaitFor(int expected, Expression<Func<T, int>> action, double timeout)
        {
            var methodCallExp = (MethodCallExpression)action.Body;
            string methodName = methodCallExp.Method.Name;
            _logAction(methodName);
            var x = action.Compile();

            var sw = new Stopwatch();
            sw.Start();
            var ms = TimeSpan.FromSeconds(timeout).TotalMilliseconds;
            while (expected != x(Page))
            {
                if (ms < sw.ElapsedMilliseconds)
                    throw new TimeoutException($"Retrieved value never got to be {expected}");

                Thread.Sleep(100);
            }

            return this;
        }

        public FluentChainBuilder<T> SetLogger(Logger logger, Action<string> log)
        {
            switch (logger)
            {
                case Logger.Action:
                    _logAction = log;
                    break;
                case Logger.Page:
                    _logPage = log;
                    break;
                case Logger.PageAction:
                    _logPageAction = log;
                    break;
                default:
                    throw new NotImplementedException($"{logger}");
            }

            return this;
        }

        IDirectResult<T> IDirectResult<T>.Do(params Expression<Action<T>>[] actions)
        {
            return Do(actions);
        }

        IOnResult<T> IOnResult<T>.SetLogger(Logger logger, Action<string> log)
        {
            return SetLogger(logger, log);
        }

        IDirectResult<T> IAndResult<T>.Do(params Expression<Action<T>>[] actions)
        {
            return Do(actions);
        }

        IDirectResult<T> IOnResult<T>.Do(params Expression<Action<T>>[] actions)
        {
            return Do(actions);
        }

        IOnResult<T> IOnResult<T>.SetEvent(Event e, Action action)
        {
            switch (e)
            {
                case Event.OnResolve:
                    _onResolveEvent = action;
                    break;
                default:
                    throw new NotImplementedException($"{e}");
            }

            return this;
        }
    }
}
