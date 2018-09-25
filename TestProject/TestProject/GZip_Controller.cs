using System;
using System.IO;
using System.Threading;

namespace GZipTest
{
    public class GZip_Controller
    {
        //необходимые переменные

        //данные неархивированного файла
        public byte[][] data;
        //данные заархивированного файла
        public byte[][] data_gz;
        //длина блока для сжатия
        public int block_size = 1048576;
        //количество допустимых потоков
        public int thread_count;       
        //проверка аварийного завершения работы программы
        public bool error = false;

        //архивация файла
        //потоки
        public FileStream in_stream;
        public FileStream out_stream;
        //замки

        public static object lock1 = new object();
        public static object lock2 = new object();
        public static object lock3 = new object();
        public static int locker1 = 0;
        public static int locker2 = 0;
        public bool[] check;

        //базовый конструктор
        public GZip_Controller()
        {
            thread_count = Environment.ProcessorCount;
            data = new byte[thread_count][];
            data_gz = new byte[thread_count][];
        }

        //проверка  существования файла с таким названием
        public static bool ExistFile(string way)
        {
            if(File.Exists(way))
            {
                Console.WriteLine("По указанному пути: \"" + way + "\" уже существует файл с таким названием. Пожалуйста, укажите другое имя.");
                return true;
            }
            return false;
        }

        //проверка финального результата для очистки от мусора, если он есть
        public void Check(string way)
        {
            if (File.Exists(way) && error) File.Delete(way);
        }


        public void GZIp(string way_in, string way_out)
        {
            //проверяем нет ли файла с таким же названием как у нашего архива
            if (!ExistFile(way_out))
                try
                {
                    //открываем потоки для архивируемого файла и будущего архива
                    using (in_stream = new FileStream(way_in, FileMode.Open))
                    {
                        using (out_stream = new FileStream(way_out, FileMode.Append))
                        {


                            //считаем количество необходимых потоков                           
                            threadcount(in_stream.Length);
                            //заполняем массив для условий закрытия потоков чтения/записи файла
                            createcheckarray(thread_count);
                            //запускаем потоки
                            Threads(thread_count);

                            //крутим цикл пока потоки для сжатия/разжатия не завершат работу
                            while (Checked(check))
                            { }
                            GC.Collect();
                        }
                    }

                }
                catch (Exception ex)
                {
                    //обрабатываем ошибки
                    Console.WriteLine("Произошла следующая ошибка: " + ex.Message);
                    error = true;
                    Check(way_out);
                }


        }
        //считаем количество необходимых потоков  
        //если файлу для обработки требуется меньше потоков чем может предоставить процессор, открываем лишь нужное число
        private void threadcount(long file_length)
        {
            int temp_count = Convert.ToInt32(file_length % block_size) + 1;
            if (temp_count < thread_count) thread_count = temp_count;
        }

        //заполняем массив для условий закрытия потоков чтения/записи файла
        //создаем bool массив, в который каждый из открытых нами Threadов будет "отчитываться" что он завершил работу.
        private void createcheckarray(int array_length)
        {
            check = new bool[array_length];
            for (int i = 0; i < array_length; i++)
            {
                check[i] = true;
            }
        }
        //проверяем в основном потоке завершили ли Thread свою работу. До тех пор, пока есть хоть один работающий Thread возвращает true
        private bool Checked(bool[] check)
        {
            int i = 0;
            for (int j = 0; j < thread_count; j++)
            {
                if (!check[j]) i++;

            }
            if (i == thread_count) return false;
            else return true;
        }
        //создаем массив потоков по числу thread_count и запускаем их
        private void Threads(int thread_count)
        {
            Thread[] threads = new Thread[thread_count];
            for (int i = 0; i < thread_count; i++)
            {
                threads[i] = new Thread(CompressBlocks);
                threads[i].Name = i.ToString();
                threads[i].IsBackground = true;
                threads[i].Start(i);
            }
        }
        public virtual void CompressBlocks(object i)
        {

        }

    }
}
