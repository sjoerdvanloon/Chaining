using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading;

namespace Chaining
{
    public interface IPage
    {

    }
    public class DotDotPage : IPage
    {
        public void FillIn() {
            Trace.WriteLine("Ola, fill in");
        }

        public void Logon()
        {
            Trace.WriteLine("Ola, llogon");

        }

        public void UseParameters(int x)
        {
            Trace.WriteLine($"{x}");

        }

        private int _x = 2;
        public int GetValue()
        {
            _x++;
            return _x;
        }
    }

    public class Imp<T> where T : IPage, new()
    {

        public Builder<T> Builder = new Builder<T>();
        public Imp<T> On() 
        {
            var page = (T)Activator.CreateInstance(typeof(T));
            Builder.Page = page;

            return this;
        }

        //public Imp<T> Do(Action<T> a)
        //{
        //    Builder.Actions.Add(a);

        //    return this;
        //}

        public Imp<T> Do(Expression<Action<T>> a)
        {
            var methodCallExp = (MethodCallExpression)a.Body;
            string methodName = methodCallExp.Method.Name;
            Trace.WriteLine($"Added {methodName}");
            var x = a.Compile();
            Builder.Actions.Add(x);

            return this;
        }

        public Imp<T> Do2(params Expression<Action<T>>[] a)
        {
                     return this;
        }

        public Imp<T> And { get { return this; } }


        public Imp<T> Until(int expected, Expression<Func<T,int>> a, double timeout = 10)
        {
            Directly();

            var methodCallExp = (MethodCallExpression)a.Body;
            string methodName = methodCallExp.Method.Name;
            Trace.WriteLine($"Added {methodName}");
            var x = a.Compile();

            var sw = new Stopwatch();
            sw.Start();
            var ms = TimeSpan.FromSeconds(timeout).TotalMilliseconds;
            while(expected != x(Builder.Page))
            {
                if (ms < sw.ElapsedMilliseconds)
                    throw new TimeoutException($"Retrieved value never got to be {expected}");

                Thread.Sleep(100);
            }

            //await WaitUntil(expected, x, timeout);

            //var waitTask = Task.Run(async () =>
            //{
            //    while (x(Builder.Page) != expected) await Task.Delay(100);
            //});

            //if (waitTask != await Task.WhenAny(waitTask,
            //             Task.Delay((int)(timeout * 1000))))
            //    throw new TimeoutException();

            return this;

        }

        public void Directly()
        {
            foreach (var action in Builder.Actions)
            {
                action.Invoke(Builder.Page);
               Trace.WriteLine(action.Method.Name);
            }
        }
    }

    public class Actions
    {

    }

    public class Builder<T>
        {
        public T Page { get; set; }
        public List<Action<T>> Actions { get; set; } = new List<Action<T>>();
        }

    //public static class Ext
    //{
    //    public static Imp<T> On<T>() where T : IPage, new()
    //    {
    //        var imp = new Imp<T>();
    //        return imp.On();
    //    }
    //}

    public class UnitTest1
    {
        private static Imp<T> On<T>() where T : IPage, new()
        {
            var imp = new Imp<T>();
            return imp.On();

        }

        [Fact]
        public  void Test1()
        {       
            var i = On<DotDotPage>().Do(x => x.FillIn()).And.Do(x => x.Logon()).Until(5, x => x.GetValue());
            On<DotDotPage>().Do2(x => x.FillIn(), y => y.FillIn());

            i.Builder.Page.Should().NotBeNull().And.BeOfType<DotDotPage>();
            i.Builder.Actions.Should().HaveCount(2);
        }
    }
}
