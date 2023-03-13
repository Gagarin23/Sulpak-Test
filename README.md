# Sulpak-Test

Задание:<br>
в базе данных (sql) лежат записи (скажем заказы, поля можно придумать) их много, регулярно появляются много новых записей, и так же присутствуют постоянные операции обновления строк(их не много).
то есть имеем:
1.	много вставок в бд
2.	еще больше чтений данных (пусть будет прям на порядок больше, чем вставок)

нужно сделать сервис цель, которого – максимально в короткое время обрабатывать новые записи, потому в теории 
инстансов сервиса может быть много, чтобы уверенно успевать обрабатывать новые записи с минимальной задержкой
использовать .net 7
важно продемонстрировать при решении задачи применяя применения СОЛИД принципов (особенно С и Д)


нужно сделать джобик (сервис):
1.	брать новые заказы, те, что только что были добавлены (вариант: в определённом статусе пусть у нас есть такое поле или с пустым каким-то флагом)
2.	выполнить с заказом какую-то не быструю операцию (до минуты – можно для симуляции работы просто встать в рандомную задержку)
3.	в зависимости от результата предыдущего шага (возвращать рандомно) либо проставить новый статус (или заполнить флаг), либо вернуть начальный из шага 1
важно:
1.	джобиков несколько, и они работают одновременно – поэтому важно обеспечить конкурентную работу, а именно атомарность в обработке заказа между инстансами
2.	и чтобы если заказ уже кто-то обрабатывает, его больше никто не мог взять из джобиков в работу – один заказ может единовременно 
обрабатываться только одним джобиком, но при этом он все еще должен быть доступен для чтения из бд, то есть вариант длительной блокировки на уровне бд не подходит
3.	блокировки на уровне бд если будут, то должны быть кратковременными, иначе мы не сможем читать и обновлять данные  – а читателей много
задача:
1.	реализовать джобик 
2.	запустить несколько инстансов джобиков и они должны не мешать друг другу, при этом работать тоже должны все одновременно
3.	дополнительно симулировать параллельно с работой джобиков постоянную вставку новых заказов (лучше отдельным приложением)
4.	дополнительно симулировать параллельно с работой джобиков постоянное чтение заказов по всей таблице, то есть как новых, так и старых (лучше отдельным приложением)

дополнительные требования:
1.	код должен быть красивым и легко читаемым, на поясняющие комменты не скупимся, но в идеале правильное называние метода лучше
2.	применение букв С и Д из солид принципов настоятельно приветствуется – за это будет отдельный плюс
3.	как реализовать атомарно конкурентный доступ к заказам, тут можно предложить несколько вариантов – главное помнить, что джобиков единовременно всегда несколько
4.	при необходимости можно добавить поля в таблице заказов или даже таблицы в бд
5.  одним из критериев хорошего решения является то которое с минимальными изменениями позволит реализовать другие виды сервисов обработки заказов, то есть выстроить в будущем 
    этакий пайплайн обработки, при чем не всегда линейно-последовательное 
    пример: создание заказа, постановка в резрв товаров заказов на складе, прием платежей по заказу,
    отправка уведомления клиенту, формирование чека, формирование заявки на доставку и т.д.)

# Решение:
Запуск:<br>
docker-compose up -d<br>
Через планировщик запускаем по 3 джобы по адресам ...:5001/hangfire/recurring, ...:6001/hangfire/recurring<br>
...:3000 - графана. Логин/пароль admin/admin. Добавил базовые дашборды:<br>
![image](https://user-images.githubusercontent.com/59282770/224540039-7a680c0a-c5f1-4022-9c48-5a163545fcc5.png)<br>

Контейнеры:<br>
2 ноды - проект с заданием<br>
mssql - база<br>
jaeger - сборщик трассировок<br>
elasticsearch - хранение трассировок<br>
grafana - графики на основе собранных трассировок<br>

Объяснение:<br>
Логика с решением расположена в слое Application, папка Orders. Папки:<br>
Processed - имитация обработки, которая требовалась в задании. Через планировщик настроена обработка один раз в секунду всех заказов в статусе "Не обработан". https://github.com/Gagarin23/Sulpak-Test/blob/master/src/Application/Orders/Processed/Commands/OrdersProcessedCommandHandler.cs<br>
Create - создание заказа. Через планировщик настроено параллельное создание 5-ти заказов в секунду. https://github.com/Gagarin23/Sulpak-Test/blob/master/src/Application/Orders/Create/Commands/CreateOrderCommandHandler.cs<br>
Get - получение записей заказов. Через планировщик настроено получение через пагинацию 30-ти заказов, в 50 параллельных запросов каждую секунду. 1% запросов вызывает искусственную ошибку. https://github.com/Gagarin23/Sulpak-Test/blob/master/src/Application/Orders/Get/Queries/GetOrdersQueryHandler.cs<br>

Основная реализация:<br>
Чистая архитектура + CQRS с помощью медиатора.<br>
Подробное объяснение реализации описано комментариями в исходном коде.<br>