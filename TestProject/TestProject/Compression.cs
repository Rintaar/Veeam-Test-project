using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
    public class Compression : GZip_Controller
    {

        //основной метод для работы с данными. Имеет три замка для синхронизации с основным потоком:
        //lock1 - для того чтобы взять с in_stream новый блок данных
        //lock2 - для того чтобы записать в out_stream новый блок данных
        //lock3 - для того чтобы отчитаться о завершении своей работы
        //архивация/разархивация данных происходит между lock1 и lock2
        //используя locker1, locker2 создаем искуственную очередь для потоков, где будет пропущен только тот поток чей index(имя) совпадет с locker.
        //остальные готовые идти дальше будут усыплены на thread_count ms и дальше останутся ждать своей очереди
        public override void CompressBlocks(object i)
        {
            //получаем номер потока
            int index = Convert.ToInt32(Thread.CurrentThread.Name.ToString());  
            //массивы для данных
            byte[] data = new byte[1024];
            byte[] zipdata;
            //условие выхода из цикла
            //0 - цикл крутится
            //1 - последняя итерация
            //2 - немедленное завершение
            int exit = 0;

            while (exit<1)
            {
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
                            data = GetBlocks(exit, data, in_stream);
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
                    //прогоняем через Gzip текущий кусок данных. Итог записываем в zipdata
                    zipdata = Zipper(data);                    
                    //по аналогии выше используя lock2 и locker2 записываем новый кусок в файл
                    while (true)
                    {
                        lock (lock2)
                        {
                            if (locker2 == index)
                            {
                                CreateFile(zipdata, out_stream);
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

        //получаем новый блок данных. попутно, если массив кончился меняем состояние контроллера exit
        private byte[] GetBlocks(int exit, byte[] data, FileStream fs)
        {
            int block_length;
            if (fs.Length - fs.Position >= block_size) block_length = block_size;
            else
            {
                block_length = Convert.ToInt32(fs.Length - fs.Position);
                exit = 1;
            }
            data = new byte[block_length];
            //long op = in_stream.Position;                           
            fs.Read(data, 0, block_length);
            //long np = in_stream.Position;
            //Console.WriteLine(String.Format("Поток: {0} блоки с {1} по {2} из {3}", Thread.CurrentThread.Name.ToString(), op, np, in_stream.Length));
            return data;
        }

        private byte[] Zipper(byte[] data)
        {
           
            using (MemoryStream output = new MemoryStream(data.Length))
            {
                using (BufferedStream compress = new BufferedStream(new GZipStream(output, CompressionMode.Compress), 8192))
                {
                    compress.Write(data, 0, data.Length);
                    compress.Dispose();
                }
                if (data.Length > output.ToArray().Length)
                    return output.ToArray();
                else
                {
                    List<byte> temp = new List<byte>();
                    //прописываем байты избыточного кода 
                    temp.AddRange(new byte[] { 111, 111, 111, 111 });
                    //прописываем байты о размере
                    temp.AddRange(BitConverter.GetBytes(data.Length + 8));
                    //копируем наши исходные данные
                    temp.AddRange(data);
                    return temp.ToArray();
                }
            }
        }
        private void CreateFile(byte[] zipdata, FileStream fs)
        {
            BitConverter.GetBytes(zipdata.Length + 1).CopyTo(zipdata, 4);
            fs.Write(zipdata, 0, zipdata.Length);
        }
    }
}
