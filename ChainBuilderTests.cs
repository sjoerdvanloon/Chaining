using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Chaining
{


    public class ChainBuilderTest
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
