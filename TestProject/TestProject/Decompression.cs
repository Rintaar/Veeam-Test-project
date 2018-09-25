using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;


namespace GZipTest
{
    public class Decompression : GZip_Controller
    {
        ////основной метод для работы с данными. Имеет три замка для синхронизации с основным потоком:
        ////lock1 - для того чтобы взять с in_stream новый блок данных
        ////lock2 - для того чтобы записать в out_stream новый блок данных
        ////lock3 - для того чтобы отчитаться о завершении своей работы
        ////архивация/разархивация данных происходит между lock1 и lock2
        ////используя locker1, locker2 создаем искуственную очередь для потоков, где будет пропущен только тот поток чей index(имя) совпадет с locker.
        ////остальные готовые идти дальше будут усыплены на thread_count ms и дальше останутся ждать своей очереди
        private bool checker(byte[] buf)
        {
            if (buf[0] == buf[1] && buf[1] == buf[2] && buf[2] == buf[3] && buf[0] == 111)
                return true;
            else return false;
        }
        public override void CompressBlocks(object i)
        {
            //получаем номер потока
            int index = Convert.ToInt32(Thread.CurrentThread.Name.ToString());
            //массивы для данных
            byte[] data = new byte[1024];
            byte[] zipdata = new byte[1024];
            bool compressed;
            //условие выхода из цикла
            //0 - цикл крутится
            //1 - последняя итерация
            //2 - немедленное завершение
            int exit = 0;

            while (exit < 1)
            {
                compressed = true;
                //получаем новый блок данных крутим цикл пока не получим данные или они не кончатся
                while (true)
                {
                    lock (lock1)
                    {
                        //если данных нет, то выходим из цикла сразу
                        if (in_stream.Position >= in_stream.Length)
                        {
                            exit = 2;
                            break;
                        }
                        //используем locker1
                        if (locker1 == index)
                        {
                            //создаем массив для хранения информации о размере
                            byte[] buf = new byte[8];
                            //считываем байты в буфер - 4 байта об избыточном коду, 4 - о размере
                            in_stream.Read(buf, 0, 8);
                            //устанавливаем длину блока
                            int block_length = BitConverter.ToInt32(buf, 4);
                            //если размер буфера совпадет с размером исходного блока - у нас не сжатый пакет идет.
                            //Значит нет смысла его пытаться разархивировать compressed = false;
                            //также проверяем байты в избыточном коде: дополнительная проверка для целого пакета и основная для тех кусков, что в конце файла и размером меньше чем размер пакета
                            if (block_length-1 == block_size || (buf[0] == buf[1] && buf[0] == buf[2] && buf[0] == buf[3] && buf[0] == 111))
                            {
                                //создаем массив на основе полученных размером
                                zipdata = new byte[block_length - 9];
                                in_stream.Read(zipdata, 0, zipdata.Length);
                                compressed = false;
                                data = zipdata;
                            }
                            else
                            {
                                //создаем массив на основе полученных размером
                                zipdata = new byte[block_length - 1];
                                //переносим в них данные с буфера
                                buf.CopyTo(zipdata, 0);
                                //читаем данные указанной длины минус 8 прочитанных байт из буфера в массив 
                                in_stream.Read(zipdata, 8, zipdata.Length - 8);
                                int decomp_block_length = BitConverter.ToInt32(zipdata, zipdata.Length - 4);
                                //создаем массив для распакованного блока
                                data = new byte[decomp_block_length];
                            }
                           
                           
                            if (locker1 == thread_count - 1) locker1 = 0;
                            else locker1++;
                            break;
                        }
                        else Thread.Sleep(thread_count);
                    }

                }
                //если мы получили данные идем дальше.
                if (exit < 2)
                {
                    if (compressed)
                    {
                        //прогоняем через Gzip текущий кусок данных. Итог записываем в zipdata
                        using (MemoryStream output = new MemoryStream(zipdata))
                        {
                            using (GZipStream decompress = new GZipStream(output, CompressionMode.Decompress))
                            {
                                decompress.Read(data, 0, data.Length);
                            }
                        }
                    }
                    //по аналогии выше используя lock2 и locker2 записываем новый кусок в файл
                    while (true)
                    {
                        lock (lock2)
                        {
                            if (locker2 == index)
                            {
                                out_stream.Write(data, 0, data.Length);
                                if (locker2 == thread_count - 1) locker2 = 0;
                                else locker2++;
                                break;
                            }
                            else Thread.Sleep(thread_count);
                        }
                    }
                }
            }
            //если работа окончена - закрываем поток 
            lock (lock3)
            {
                check[index] = false;
                GC.Collect();
            }

        }

    }
}

