using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Chaining
{


    public class FluentChainBuilderTest
    {
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


            public int Number { get; private set; }

            public void AddNumber(int add = 1)
            {
                Number += add;
            }

            private int _x = 2;
            public int GetValue()
            {

                _x++;
                Log($"Getting value {_x}");
                return _x;
            }
        }

        private readonly ITestOutputHelper output;
        private int _logActionCount = 0;
        private int _onResolveCount = 0;

        public FluentChainBuilderTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        private IOnResult<T> On<T>() where T : IPage, new()
        {
            _logActionCount = 0;
            _onResolveCount = 0;

            var builder =
                    FluentChainBuilder<T>.On()
                    .SetLogger(Logger.Page, x =>
                    {
                        var t = $"Page {x}";
                        output.WriteLine(t); output.WriteLine(new string('-', t.Length));
                    })
                    .SetLogger(Logger.Action, x =>
                    {
                        _logActionCount++;
                        output.WriteLine($" - {x}");
                    })
                    .SetLogger(Logger.PageAction, x => output.WriteLine($"   - {x}"))
                    .SetEvent(Event.OnResolve, () => _onResolveCount++);

            return builder;
        }
        #region "On"
        [Fact]
        public void On_ShouldNotDirectlyResolvePage()
        {
            var i = On<DotDotPage>();

            var builder = (FluentChainBuilder<DotDotPage>)i;
            builder.Should().NotBeNull().And.BeOfType<FluentChainBuilder<DotDotPage>>();
            builder.Page.Should().BeNull();
            _onResolveCount.Should().Be(0);
        }

        #endregion

        #region "SetLogger"

        [Theory]
        [InlineData(Logger.Action)]
        [InlineData(Logger.PageAction)]
        [InlineData(Logger.Page)]
        public void SetLogger_ShouldNotDirectlyResolvePage(Logger logger)
        {
            var i = On<DotDotPage>().SetLogger(logger, (x) => { });

            var builder = (FluentChainBuilder<DotDotPage>)i;
            builder.Page.Should().BeNull();
            _onResolveCount.Should().Be(0);
        }

        #endregion
        #region
        [Fact]
        public void Do_ShouldResolvePageFirstTime()
        {
            var i = On<DotDotPage>().Do(p => p.AddNumber(2));

            var builder = (FluentChainBuilder<DotDotPage>)i;
            builder.Page.Should().NotBeNull();
            _onResolveCount.Should().Be(1);
        }

        [Fact]
        public void Do_ShouldExecuteDirectly()
        {
            var x = 0;
            var i = On<DotDotPage>().Do(p => p.AddNumber(2));

            var builder = (FluentChainBuilder<DotDotPage>)i;
            builder.Page.Should().NotBeNull();
            builder.Page.Number.Should().Be(2);
        }

        [Fact]
        public void Do_ShouldNotResolvePageAfterFirstTime()
        {
            var x = 0;
            var i = On<DotDotPage>().Do(p => p.FillIn()).Do(p => p.FillIn());

            var builder = (FluentChainBuilder<DotDotPage>)i;
            builder.Page.Should().NotBeNull();
            _onResolveCount.Should().Be(1);
        }
        #endregion

        [Fact]
        public void Until_ShouldWorkWithDefaultValues()
        {
            var i =
                On<DotDotPage>()
                .Do(x => x.FillIn())
                .And.Do(x => x.Logon())
                .WaitFor(5, x => x.GetValue());

            var builder = (FluentChainBuilder<DotDotPage>)i;
            builder.Page.Should().NotBeNull().And.BeOfType<DotDotPage>();
            _logActionCount.Should().Be(3);
        }

        [Fact()]
        public void Do_ShouldWorkWithMultipleActions()
        {
            var i = On<DotDotPage>().Do(
                x => x.FillIn(),
                x => x.Logon(),
                x => x.FillIn(),
                x => x.Logon());

            var builder = (FluentChainBuilder<DotDotPage>)i;
            builder.Page.Should().NotBeNull().And.BeOfType<DotDotPage>();
            _logActionCount.Should().Be(4);
        }
    }
}
