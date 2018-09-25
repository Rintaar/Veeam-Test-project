using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //try
            //{
            //    switch (args[0])
            //    {
            //        case "compress":
            //            compress(args[1], args[2]);
            //            break;
            //        case "decompress":
            //            decompress(args[1], args[2]);
            //            break;
            //        case "help":
            //            help();
            //            break;
            //        case "about":
            //            about();
            //            break;
            //        default:
            //            help();
            //            break;
            //    }
            //}
            //catch
            //{
            //    help();
            //}
            Compression c = new Compression();
            Console.WriteLine("Архивация запущена");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            c.GZIp(@"C:/test/test.mkv", @"C:/test/test.mkv.gz");
            sw.Stop();
            Console.WriteLine("Время выполнения " + (sw.ElapsedMilliseconds / 100.0).ToString());
            Console.WriteLine("Архивация завершена");

            Decompression cс = new Decompression();
            Console.WriteLine("Архивация запущена");
            Stopwatch swc = new Stopwatch();
            swc.Start();
            cс.GZIp(@"C:/test/test.mkv.gz", @"C:/test/test11.mkv");
            swc.Stop();
            Console.WriteLine("Время выполнения " + (swc.ElapsedMilliseconds / 100.0).ToString());
            Console.WriteLine("Архивация завершена");

        }
        private static void compress(string way_in, string way_out)
        {
            Compression c = new Compression();
            Console.WriteLine("Архивация запущена");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            c.GZIp(way_in, way_out);
            sw.Stop();
            Console.WriteLine("Время выполнения "+(sw.ElapsedMilliseconds / 100.0).ToString());
            Console.WriteLine("Архивация завершена");
        }
        private static void decompress(string way_in, string way_out)
        {
            Console.WriteLine("Разархивация запущена");
            Decompression de = new Decompression();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            de.GZIp(way_in, way_out);
            sw.Stop();
            Console.WriteLine("Время выполнения " + (sw.ElapsedMilliseconds / 100.0).ToString());
            Console.WriteLine("Разархивация завершена");
        }
        private static void help()
        {
            Console.WriteLine("Справка по использованию архиватора:\n" +
                "GZipTest compress [файл 1] [файл 2]\t - сжатие файла1 в архив ''файл2'' \n" +
                "GZipTest decompress [файл 1] [файл 2]\t - распаковка архива  ''файл1'' в файл2\n" +
                "GZipTest help\t\t\t\t - вывод справки\n" +
                "GZipTest about\t\t\t\t - вывод информации об исполнителе и использованных материалах\n");
        }
        private static void about()
        {
            Console.WriteLine("Задание выполнил Алексеев Ю.А. 21.07.2018 - 09.08.2018");
        }
    }
}