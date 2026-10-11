## RTM на рівні вимог

| Вимога | Use Case | Критерії приймання | Проєктні артефакти |
|--------|----------|--------------------|--------------------|
| FR-01 Перегляд подій | UC-01 | AC-01.1–AC-01.4 | Use Case Diagram; Activity UC-01; Class Diagram (Event) |
| FR-02 Створення події | UC-02 | AC-02.1–AC-02.4 | Use Case Diagram; Activity UC-02; Class Diagram (User, Event) |
| FR-03 Бронювання | UC-03 | AC-03.1–AC-03.3 | Use Case Diagram; Activity UC-03; Class Diagram (User, Order, Event) |
| FR-04 Квиток з токеном і QR-кодом | UC-03 | AC-04.1–AC-04.4 | Activity UC-03; Class Diagram (Ticket); State Machine Ticket |
| FR-05 Перевірка квитка | UC-04 | AC-05.1–AC-05.4 | Use Case Diagram; Activity UC-04; Class Diagram (Ticket); State Machine Ticket |
| FR-06 Журнал BookingHistory | UC-03 | AC-06.1–AC-06.2 | Activity UC-03 |
| FR-07 Скасування замовлення | UC-05 | AC-07.1–AC-07.2 | Class Diagram (Order); Use Case Diagram; State Machine Ticket |
| NFR-01 Час відповіді | UC-01, UC-04 | AC-N1.1–AC-N1.2 | План перевірки у ЛР №4 |
| NFR-02 Відсутність овербукінгу | UC-03 | AC-N2.1–AC-N2.2 | План перевірки у ЛР №4 |
