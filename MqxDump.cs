using System;

namespace Tso2MqoGui
{
    static class MqxDump
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("MqxDump.exe <mqo file>");
                return;
            }
            string mqo_file = args[0];

            MqxReader reader = new MqxReader();
            reader.MqoFile = mqo_file;
            reader.Read();
        }
    }
}
