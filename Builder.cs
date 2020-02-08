using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;

namespace Chaining
{
    public class Builder<T> where T : IPage, new()
    {
        Action<string> logPage;
        Action<string> logAction;
        Action<string> logPageAction;
        public T Page { get; set; }

        public Builder(Action<string> logPage, Action<string> logAction, Action<string> logPageAction)
        {
            this.logPage = logPage;
            this.logAction = logAction;
            this.logPageAction = logPageAction;

            logPage(typeof(T).Name);
            Page = (T)Activator.CreateInstance(typeof(T));
            Page.Log = logPageAction;
        }

        public Builder<T> Do(params Expression<Action<T>>[] actions)
        {
            foreach (var action in actions)
            {
                var methodCallExp = (MethodCallExpression)action.Body;
                string methodName = methodCallExp.Method.Name;
                logAction(methodName);
                var x = action.Compile();
                x.Invoke(Page);
            }

            return this;
        }

        public Builder<T> And { get { return this; } }
        
        public Builder<T> Until(int expected, Expression<Func<T, int>> a, double timeout = 10)
        {
            var methodCallExp = (MethodCallExpression)a.Body;
            string methodName = methodCallExp.Method.Name;
            logAction(methodName);
            var x = a.Compile();

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
    }
}
