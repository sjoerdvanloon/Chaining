using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Chaining
{
    public interface IPage
    {
        Action<string> Log { get; set; }
    }
    public class DotDotPage : IPage
    {
        public Action<string> Log { get; set; }

        public void FillIn()
        {
            Log($"Ola, fill in");

        }

        public void Logon()
        {
            Log($"Ola, llogon");

        }

        public void UseParameters(int x)
        {
            Trace.WriteLine($"{x}");

        }

        private int _x = 2;
        public int GetValue()
        {

            _x++;
            Log($"Getting value {_x}");
            return _x;
        }
    }

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

    public class ChainBuilderTest
    {
        private readonly ITestOutputHelper output;
        private int _logActionCount = 0;

        public ChainBuilderTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        private Builder<T> On<T>() where T : IPage, new()
        {
            _logActionCount = 0;
            var imp = new Builder<T>(
                x =>
                {
                    var t = $"Page {x}";
                    output.WriteLine(t); output.WriteLine(new string('-', t.Length));
                },
                x =>
                {
                    _logActionCount++;
                    output.WriteLine($" - {x}");
                },
                x => output.WriteLine($"   - {x}"));
            return imp;

        }

        [Fact]
        public void Until_ShouldWorkWithDefaultValues()
        {
            var i = On<DotDotPage>().Do(x => x.FillIn()).And.Do(x => x.Logon()).Until(5, x => x.GetValue());

            i.Page.Should().NotBeNull().And.BeOfType<DotDotPage>();
            _logActionCount.Should().Be(3);
        }

        [Fact()]
        public void Do_ShouldWorkWithMultipleActions()
        {
            var y = On<DotDotPage>().Do(x => x.FillIn(), y => y.FillIn(), y => y.FillIn(), y => y.FillIn());

            y.Page.Should().NotBeNull().And.BeOfType<DotDotPage>();
            _logActionCount.Should().Be(4);
        }
    }
}
