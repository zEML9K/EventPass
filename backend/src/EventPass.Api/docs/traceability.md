# Матриця простежуваності вимог (Traceability Matrix)

| ID Вимоги | Назва вимоги / Критерій (AC) | Компонент / Клас | Метод C# / Рядок коду | xUnit Тест                                               | Статус |
| :--- | :--- | :--- | :--- |:---------------------------------------------------------| :--- |
| **FR-01** | Перегляд каталогу подій та вільних місць | `Program.cs` | `app.MapGet("/api/events")` | -                                                        | **Passed** |
| **FR-03 / NFR-02** | Атомарне бронювання місць та списання залишку | `BookingService` | `ev.AvailableSeats -= request.Quantity;` | `BookTickets_ShouldSucceed_WhenSeatsAreAvailable`        | **Passed** |
| **AC-03** | Блокування бронювання при браку місць | `BookingService` | `BookingResult.Fail("insufficient_seats")` | `BookTickets_ShouldFail_WhenNotEnoughSeats`              | **Passed** |
| **FR-04** | Випуск квитків із токеном та статусом `Valid` | `BookingService` | `Token = Guid.NewGuid()`, `Status = TicketStatus.Valid` | `BookTickets_ShouldGenerateUniqueTokens_WithValidStatus` | **Passed** |
| **FR-05** | Перевірка квитка та зміна статусу на `Scanned` | `BookingService` | `ticket.Status = TicketStatus.Scanned` | `CheckTicket_ShouldSucceed_AndChangeStatusToScanned`     | **Passed** |
| **AC-05** | Захист від повторного проходу (уже `Scanned`) | `BookingService` | `CheckTicketResult.Fail("ticket_already_used")` | `CheckTicket_ShouldFail_WhenTicketIsAlreadyScanned`      | **Passed** |
| **FR-06** | Фіксація дій у журналі аудиту | `BookingService` | `_db.BookingHistories.Add(history)` | `BookTickets_ShouldSucceed_WhenSeatsAreAvailable`        | **Passed** |