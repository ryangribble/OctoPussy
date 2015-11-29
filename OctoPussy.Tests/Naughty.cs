using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace OctoPussy.Tests
{
    [TestFixture]
    public class Naughty
    {
        [Test]
        public void ShouldReallyHaveSomeTests()
        {
            var amINaughty = "yes";
            amINaughty.ShouldBe("yes");
        }
    }
}
