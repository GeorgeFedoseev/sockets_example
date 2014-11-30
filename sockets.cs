// Подключаем все нужные библиотеки
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

// нэймспейс нужен, чтобы потом в другом файле можно было написать "using Sockets;"
namespace Sockets
{
    // объявляем класс клиента
    class MyClient
    {
        // Создаем переменные, которые будем использовать внутри класса:
        // Сокет клиента
        private Socket client;
        // Адрес сервера
        private IPAddress ip = IPAddress.Parse("127.0.0.1");
        // Порт, по которому будем присоединяться
        private int port = 1991;
        // Список для хранения потоков потоков (приложение многопоточное, и при создании 
        // потока, чтобы он не самоуничтожился, его надо где-то сохранить)        
        private List<Thread> threads = new List<Thread>();

        public void connnect()
        {
            // try-catch используется для отлова ошибок, если внутри блока try произошла
            // какая-то ошибка, то программа не вылетает, а выполняется блок catch
            try
            {
                // создаем сокет и кладем его в ранее объявленную переменную
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // подключаемся к серверу
                client.Connect(ip, port);
                // все ок
                Console.WriteLine("Успех подключения к серверу");
            }
            catch
            {
                // сервер не ответил
                Console.WriteLine("Ошибка подключения к серверу");
            }
        }


        // функция, которая создает отдельный поток для получения сообщений
        public void beginReceiving()
        {
            // создаем поток и прописываем в нем действия, которые он выполняет
            Thread thread = new Thread(delegate()
            {

                // он запускает бесконечныйы цикл
                // в цикле каждую итерацию мы пытаемся получить сообщение
                // пока сообщение не получено цикл не идет дальше (а ждет сообщение)
                while (true)
                {
                    // получаем длину сообщения (она записана в первых 4х байтах)
                    // (см. далее: в функциях send клиента и сервера мы сначала посылаем длину сообщения, а потом
                    // уже само сообщение)
                    // 4 байта потому что тип int требует 4 байта для записи чила
                    // т.о. мы просто полчаем число типа int, где записана длина следующего за этим сообщения
                    var lengthBytes = new byte[4];
                    client.Receive(lengthBytes);
                    // переводим массив из 4х байт в число int
                    int messageLength = BitConverter.ToInt32(lengthBytes, 0);

                    // получив сообщение с длинной слдеющего сообщения, получаем это следующее сообщение
                    // создаем массив с уже известной длинной сообщения
                    byte[] bytes = new byte[messageLength];
                    // пишем в массив из сокета
                    client.Receive(bytes);
                    if (bytes.Length != 0)
                    {
                        // если сообщение не пустое, то получаем из него строку
                        string msg = Encoding.UTF8.GetString(bytes);
                        // вызываем различные функции, которые мы объявили для получения сообщения (см. далее)
                        receive(bytes);
                        receive(msg);
                    }

                }
            });

            // мы объявили переменную потока thread и записали в нее объект потока с тем, что он делает (циклом получения)
            // теперь запустим этот поток
            thread.Start();
            // и добавим его в список потоков
            // если этого не сделать, то после завершения этой функции объект потока самоуничтожится, тк это локальная переменная
            // а если он уничтожится, то он дальше не будет выполняться
            // поэтому пихаем его в список потоков
            threads.Add(thread);
        }


        // вот здесь идут функции receive
        // virtual значит, что когда мы будем наследовать этот класс, то их можно будет переопределить:
        // например, мы создаем новый класс, "отправитель" и наследуем его от клиента (этого класса)
        // и тк здесь у нас virtual, то мы можем переопределить функции receive в наследнике
        // например мы можем вывести сообщение, которое получили
        // и вызвав функцию из этого класса (как мы сделали в потоке сверху) она вызовется не здесь, а 
        // в классе "отправитель", где она переопределена и там выведется сообщение
        protected virtual void receive(byte[] bytes)
        {
            // здесь мы тоже можем что-то написать, но смысл, если потом мы все-равно их переопределим
            // если не переопределять их в наследнике, то этот блок выполнится
        }

        // мы сделали 2 функции с одним именем 
        // но они отличаются параметрами 
        // функция выбирается в зависимости он параметра, которого мы в нее передаем
        protected virtual void receive(string message)
        {

        }

        // функция передачи сообщения, если мы передаем в нее массив байт
        public void send(byte[] bytes)
        {
            try
            {
                // сначала посылаем длину слдующего сообщения
                client.Send(BitConverter.GetBytes(bytes.Length));
                // посылаем само сообщение
                client.Send(bytes);
            }
            catch { }
        }

        public void send(string msg)
        {
            byte[] bytes;
            // переводим строку в массив байт
            bytes = Encoding.UTF8.GetBytes(msg);
            // используем уже объявленную функцию для массива байт
            send(bytes);
        }

    }

    class MyServer
    {
        // Здесь примерно тоже самое, но с некоторыми отличиями
        // К серверу может подключатсья несколько клиентов
        // Поэтому получать/отправлять сообщения мы будем каждому в отдельности
        // Объявляем переменные класса:
        // Здесь будет список наших клиентов
        private Hashtable clients;
        // Это сокет нашего сервера
        Socket listener;
        // Порт, на котором будем прослушивать входящие соединения
        int port = 1991;
        // Точка для прослушки входящих соединений (состоит из адреса и порта)
        IPEndPoint Point;
        // Список потоков
        private List<Thread> threads = new List<Thread>();


        protected void startServer()
        {
            // Создаем Hashtable размером 30 для хранения клиентов
            // Hashtable это что-то типа массива или списка (List)
            clients = new Hashtable(30);
            // Создаем сокет сервера, к которому будут подключаться клиенты
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Определяем конечную точку, IPAddress.Any означает, что наш сервер будет принимать входящие соединения с любых адресов
            Point = new IPEndPoint(IPAddress.Any, port);
            // Связываем сокет с конечной точкой
            listener.Bind(Point);
            // Начинаем слушать входящие соединения
            listener.Listen(10);

            startConnections();
        }

        // функция для подключения клиентов
        private void startConnections()
        {
            // Запускаем цикл в отдельном потоке, чтобы приложение не зависло
            Thread th = new Thread(delegate()
            {
                while (true)
                {
                    // Создаем новый сокет, по которому мы сможем обращаться к клиенту
                    // Этот цикл остановится, пока какой-нибудь клиент не попытается присоединиться к серверу
                    Socket client = listener.Accept();
                    // Теперь, обратившись к объекту client, мы сможем отсылать и принимать пакеты от последнего подключившегося пользователя.
                    // Добавляем подключенного клиента в список всех клиентов, для дальнейшей массовой рассылки пакетов
                    clients.Add(client, "");
                    // Начинаем принимать входящие пакеты от этого конкретного клиента
                    
                    startReceivingFrom(client);
                    
                }
            });
            // Приведенный выше цикл пока что не работает, запускаем поток. Теперь цикл работает.
            th.Start();
            // Добавляем его в список всех потоков, чтобы он не самоуничтожился
            threads.Add(th);
        }

        private void startReceivingFrom(Socket r_client)
        {
            Console.WriteLine("Client connected!");
            // Для каждого нового подключения, будет создан свой поток для приема пакетов
            Thread th = new Thread(delegate()
            {
                while (true)
                {
                    try
                    {
                        // получаем сообщение с длинной следующего сообщения
                        var lengthBytes = new byte[4];
                        r_client.Receive(lengthBytes);
                        int messageLength = BitConverter.ToInt32(lengthBytes, 0);

                        // получаем само сообщения
                        byte[] bytes = new byte[messageLength];
                        r_client.Receive(bytes);
                        if (bytes.Length != 0)
                        {
                            // вызываем функции приема с различными параметрами
                            // в них также передаем и сокет клиента, от которого получили сообщение
                            receive(r_client, bytes);
                            receive(r_client, Encoding.UTF8.GetString(bytes));
                        }
                    }
                    catch { }
                }
            });
            // запускаем цикл
            th.Start();
            // добавляем в список потоков
            threads.Add(th);
        }

        // здесь идут виртуальные ф-ции приема также, как и в клиенте, толльк здесь еще параметр сокета клиента,
        // от которого мы получаем сообщение
        protected virtual void receive(Socket r_client, byte[] bytes)
        {

        }

        protected virtual void receive(Socket r_client, string message)
        {

        }

        // объявим удобную функцию рассылки собщения всем клиентам
        public void sendToAll(byte[] bytes)
        {
            
            // проходимя по хэштаблице подключенных клиентов и посылаем сообщение
            foreach (DictionaryEntry clientDic in clients)
            {                
                send((Socket)clientDic.Key, bytes);
            }
        }

        // та же функция, только со строкой, а не с массивом байт (для удобства)
        public void sendToAll(string msg)
        {
            foreach (DictionaryEntry clientDic in clients)
            {
                send((Socket)clientDic.Key, msg);
            }
        }

        // а здесь идут две функции для посылки сообщений коткретным клиентам

        // с массивом байт
        public void send(Socket c_client, byte[] bytes)
        {
            try
            {
                // посылаем сначала длину
                c_client.Send(BitConverter.GetBytes(bytes.Length));
                //  а потом само сообщение
                c_client.Send(bytes);
            }
            catch
            {
                Console.WriteLine("Ошибка посылки сообщения клиенту");
            }
        }

        // со строкой
        public void send(Socket c_client, string msg)
        {
            byte[] bytes;
            // переводим в массив байт
            bytes = Encoding.UTF8.GetBytes(msg);
            // и посылаем с помощью ф-ции, объявленной выше
            send(c_client, bytes);
        }

    }




}



