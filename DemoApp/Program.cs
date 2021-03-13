using System;
using System.Linq;
using DarkLink.AutoNotify;

namespace DemoApp
{
    [Flags]
    internal enum EnumMatcherFlagsEnum
    {
        Field1 = 1,

        Field2 = 2,

        Field3 = 10,

        Field4 = Field1 | Field2,
    };

    internal enum EnumMatcherNormalEnum
    {
        Field1 = 1,

        Field2 = 2,

        Field3 = 10,

        Field4 = Field1 | Field2,
    };

    internal partial class AutoNotifyClass
    {
        [AutoNotify(UsePrivateSetter = true)]
        private float value;
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var normalEnum = EnumMatcherNormalEnum.Field4;
            var flagsEnum = EnumMatcherFlagsEnum.Field4;

            var normalField = normalEnum.Match(
                () => "field1",
                () => "field2",
                () => "field3",
                () => "field4");
            var flagsFields = flagsEnum.Match(
                () => "field1",
                () => "field2",
                () => "field3",
                () => "field4");

            //new AutoNotifyClass().Value;
        }
    }
}