using System;

using DarkLink.AutoNotify;

namespace DemoApp
{
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

            //new AutoNotifyClass().Value;
        }
    }
}