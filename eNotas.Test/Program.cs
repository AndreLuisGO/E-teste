using eNotas.Test.Core;
using eNotas.Test.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Finisar.SQLite;
using System.Data;
using System.IO;

namespace eNotas.Test
{
    class Program
    {
        static int _quantidadeItensProcessados = 0;

        static void Main(string[] args)
        {
            var vendaQueue = new NonThreadSafeVendaQueue();
            vendaQueue.Boot();

            var processThread = new Thread(new ParameterizedThreadStart(ProcessRawVenda))
            {
                IsBackground = true
            };

            var cancellationTokenSource = new CancellationTokenSource();
            processThread.Start(new Tuple<NonThreadSafeVendaQueue, CancellationToken>(vendaQueue, cancellationTokenSource.Token));

            Console.WriteLine("Pressionar qualquer tecla pra sair...");
            Console.WriteLine();

            do
            {
                Console.WriteLine(vendaQueue.Stats());
                Console.WriteLine("Quantidade de itens processados: " + _quantidadeItensProcessados);
                Thread.Sleep(TimeSpan.FromSeconds(2));
            } while (!Console.KeyAvailable);

            cancellationTokenSource.Cancel(false);

            while (processThread.ThreadState != ThreadState.Stopped)
            {
                Thread.Sleep(1000);
            }
        }

        private static void ProcessRawVenda(object syncObj)
        {
            var config = (Tuple<NonThreadSafeVendaQueue, CancellationToken>)syncObj;
            var queue = config.Item1;
            var cancellationToken = config.Item2;
            do
            {
                var rawVenda = queue.Dequeue();

                Task.Run(() =>
                {

                    if (rawVenda != null)
                    {
                        var venda = queue.ProcessAsync(rawVenda).Result;
                        Interlocked.Increment(ref _quantidadeItensProcessados);

                        ProcessarVenda(venda);
                    }
                });
            } while (!cancellationToken.IsCancellationRequested);
        }

        private static IList<Venda> repository = new List<Venda>();
        private static void ProcessarVenda(Venda venda)
        {
            var currVenda = repository.Where(x => x.Id == venda.Id).SingleOrDefault();
            if (currVenda == null)
            {
                repository.Add(venda);
                //SalvaVenda(venda);
            }
            else
            {
                //Update currVenda
                currVenda.LastModifiedAt = DateTime.UtcNow;
                currVenda.Data = venda.Data;
                currVenda.DataCompetencia = venda.DataCompetencia;
                currVenda.Cliente = venda.Cliente;
                currVenda.Produto = venda.Produto;
                currVenda.ValorTotal = venda.ValorTotal;
            }

        }

        //private static void SalvaVenda(Venda venda)
        //{
        //    if ((!File.Exists("VendasProcessadas.txt"))) //Verifica se existe o arquivo
        //    {
        //        using (FileStream fs = File.Create("VendasProcessadas.txt")) //Cria o arquivo VendasProcessadas.txt
        //        {
        //            StringBuilder sb = new StringBuilder();
        //        }
        //    }
        //}

    }
}
