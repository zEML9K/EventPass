using EventPass.Api.DTO;
using EventPass.Api.Entity;

namespace EventPass.Api.Repositories;

public sealed class InMemoryEventRepository : IEventRepository
{
    private readonly InMemoryStorage _store;

    public InMemoryEventRepository(InMemoryStorage store)
    {
        _store = store;
    }

    public Task<List<VenueDto>> GetVenuesAsync()
    {
        lock (_store.SyncLock)
        {
            var result = new List<VenueDto>();
            foreach (var v in _store.Venues)
            {
                result.Add(new VenueDto(v.Id, v.Name, v.Address, v.Capacity));
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return Task.FromResult(result);
        }
    }

    public Task<Venue?> GetVenueByIdAsync(int venueId)
    {
        lock (_store.SyncLock)
        {
            foreach (var v in _store.Venues)
            {
                if (v.Id == venueId)
                {
                    return Task.FromResult<Venue?>(v);
                }
            }
            return Task.FromResult<Venue?>(null);
        }
    }
    
    public Task<List<EventDto>> GetAllAsync(string? search = null, string? category = null)
    {
        lock (_store.SyncLock)
        {
            var filtered = new List<Event>();
            var hasSearch = !string.IsNullOrWhiteSpace(search);
            var hasCategory = !string.IsNullOrWhiteSpace(category);
            var searchTerm = hasSearch ? search!.Trim() : string.Empty;
            var categoryTerm = hasCategory ? category!.Trim() : string.Empty;

            foreach (var ev in _store.Events)
            {
                if (hasSearch)
                {
                    bool matchTitle = ev.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                    bool matchDesc = ev.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                    if (!matchTitle && !matchDesc)
                    {
                        continue;
                    }
                }

                if (hasCategory)
                {
                    if (!ev.Category.Equals(categoryTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                filtered.Add(ev);
            }

            filtered.Sort((a, b) => a.Date.CompareTo(b.Date));

            var result = new List<EventDto>();
            foreach (var ev in filtered)
            {
                result.Add(ToDto(ev));
            }

            return Task.FromResult(result);
        }
    }

    public Task<EventDto?> GetByIdAsync(int id)
    {
        lock (_store.SyncLock)
        {
            foreach (var ev in _store.Events)
            {
                if (ev.Id == id)
                {
                    return Task.FromResult<EventDto?>(ToDto(ev));
                }
            }
            return Task.FromResult<EventDto?>(null);
        }
    }

    public Task<Event> AddEventAsync(Event ev)
    {
        lock (_store.SyncLock)
        {
            ev.Id = _store.NextEventId();

            foreach (var v in _store.Venues)
            {
                if (v.Id == ev.VenueId)
                {
                    ev.Venue = v;
                    break;
                }
            }

            _store.Events.Add(ev);
            return Task.FromResult(ev);
        }
    }

    private static EventDto ToDto(Event e)
    {
        string venueName = e.Venue != null ? e.Venue.Name : string.Empty;
        string venueAddress = e.Venue != null ? e.Venue.Address : string.Empty;

        return new EventDto(
            e.Id, e.Title, e.Description, e.Date,
            e.TotalTickets, e.AvailableSeats,
            venueName, venueAddress, e.Category);
    }
}

public sealed class InMemoryBookingRepository : IBookingRepository
{
    private readonly InMemoryStorage _store;

    public InMemoryBookingRepository(InMemoryStorage store)
    {
        _store = store;
    }

    public Task<(bool Success, string? ErrorCode, Booking? Booking)> CreateBookingAsync(int eventId, int quantity)
    {
        lock (_store.SyncLock)
        {
            Event? ev = null;
            foreach (var item in _store.Events)
            {
                if (item.Id == eventId)
                {
                    ev = item;
                    break;
                }
            }

            if (ev == null)
            {
                return Task.FromResult<(bool, string?, Booking?)>((false, "event_not_found", null));
            }

            if (ev.AvailableSeats < quantity)
            {
                return Task.FromResult<(bool, string?, Booking?)>((false, "insufficient_seats", null));
            }

            ev.AvailableSeats -= quantity;

            var booking = new Booking
            {
                Id = _store.NextBookingId(),
                EventId = eventId,
                Event = ev,
                Quantity = quantity,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            for (int i = 0; i < quantity; i++)
            {
                var ticket = new QrTicket
                {
                    Id = _store.NextTicketId(),
                    BookingId = booking.Id,
                    Booking = booking,
                    Status = TicketStatus.Valid,
                    Token = Guid.NewGuid().ToString("N")
                };
                booking.Tickets.Add(ticket);
                _store.QrTickets.Add(ticket);
            }

            _store.Bookings.Add(booking);
            _store.BookingHistories.Add(new BookingHistory
            {
                Id = _store.NextHistoryId(),
                BookingId = booking.Id,
                OldStatus = null,
                NewStatus = BookingStatus.Confirmed,
                ChangedAt = DateTime.UtcNow
            });

            return Task.FromResult<(bool, string?, Booking?)>((true, null, booking));
        }
    }

    public Task<(bool Success, string? ErrorCode)> CancelBookingAsync(int bookingId)
    {
        lock (_store.SyncLock)
        {
            Booking? booking = null;
            foreach (var item in _store.Bookings)
            {
                if (item.Id == bookingId)
                {
                    booking = item;
                    break;
                }
            }

            if (booking == null)
            {
                return Task.FromResult<(bool, string?)>((false, "booking_not_found"));
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                return Task.FromResult<(bool, string?)>((false, "booking_already_cancelled"));
            }

            var validTickets = new List<QrTicket>();
            foreach (var t in booking.Tickets)
            {
                if (t.Status == TicketStatus.Valid)
                {
                    validTickets.Add(t);
                }
            }

            if (validTickets.Count == 0)
            {
                return Task.FromResult<(bool, string?)>((false, "booking_not_cancellable"));
            }

            booking.Status = BookingStatus.Cancelled;
            foreach (var t in validTickets)
            {
                t.Status = TicketStatus.Cancelled;
            }

            foreach (var ev in _store.Events)
            {
                if (ev.Id == booking.EventId)
                {
                    ev.AvailableSeats += validTickets.Count;
                    break;
                }
            }

            _store.BookingHistories.Add(new BookingHistory
            {
                Id = _store.NextHistoryId(),
                BookingId = booking.Id,
                OldStatus = BookingStatus.Confirmed,
                NewStatus = BookingStatus.Cancelled,
                ChangedAt = DateTime.UtcNow
            });

            return Task.FromResult<(bool, string?)>((true, null));
        }
    }

    public Task<List<BookingDetailsDto>> GetBookingsAsync(int? bookingId = null, Func<string, string>? qrGenerator = null)
    {
        lock (_store.SyncLock)
        {
            var matchedBookings = new List<Booking>();
            foreach (var b in _store.Bookings)
            {
                if (bookingId == null || b.Id == bookingId.Value)
                {
                    matchedBookings.Add(b);
                }
            }

            matchedBookings.Sort((a, b) =>
            {
                int cmp = b.CreatedAt.CompareTo(a.CreatedAt);
                return cmp != 0 ? cmp : b.Id.CompareTo(a.Id);
            });

            var result = new List<BookingDetailsDto>();
            foreach (var b in matchedBookings)
            {
                var sortedTickets = new List<QrTicket>(b.Tickets);
                sortedTickets.Sort((a, c) => a.Id.CompareTo(c.Id));

                var ticketDtos = new List<TicketDto>();
                foreach (var t in sortedTickets)
                {
                    string? qr = (t.Status == TicketStatus.Valid && qrGenerator != null)
                        ? qrGenerator(t.Token)
                        : null;
                    ticketDtos.Add(new TicketDto(t.Id, t.Token, t.Status.ToString(), qr));
                }

                var matchedHistory = new List<BookingHistory>();
                foreach (var h in _store.BookingHistories)
                {
                    if (h.BookingId == b.Id)
                    {
                        matchedHistory.Add(h);
                    }
                }

                matchedHistory.Sort((a, c) =>
                {
                    int cmp = a.ChangedAt.CompareTo(c.ChangedAt);
                    return cmp != 0 ? cmp : a.Id.CompareTo(c.Id);
                });

                var historyDtos = new List<BookingHistoryDto>();
                foreach (var h in matchedHistory)
                {
                    historyDtos.Add(new BookingHistoryDto(h.OldStatus?.ToString(), h.NewStatus.ToString(), h.ChangedAt));
                }

                string eventTitle = b.Event != null ? b.Event.Title : string.Empty;
                result.Add(new BookingDetailsDto(
                    b.Id, b.EventId, eventTitle, b.CreatedAt,
                    b.Quantity, b.Status.ToString(), ticketDtos, historyDtos));
            }

            return Task.FromResult(result);
        }
    }

    public Task<(bool Success, string? ErrorCode, TicketDto? Ticket)> CheckTicketAsync(string token)
    {
        lock (_store.SyncLock)
        {
            QrTicket? ticket = null;
            foreach (var t in _store.QrTickets)
            {
                if (t.Token == token)
                {
                    ticket = t;
                    break;
                }
            }

            if (ticket == null)
            {
                return Task.FromResult<(bool, string?, TicketDto?)>((false, "ticket_not_found", null));
            }

            if (ticket.Status == TicketStatus.Valid)
            {
                ticket.Status = TicketStatus.Scanned;
                ticket.ScannedAt = DateTime.UtcNow;
                return Task.FromResult<(bool, string?, TicketDto?)>((true, null, new TicketDto(ticket.Id, ticket.Token, ticket.Status.ToString())));
            }

            string error = ticket.Status == TicketStatus.Cancelled ? "ticket_cancelled" : "ticket_already_used";
            return Task.FromResult<(bool, string?, TicketDto?)>((false, error, null));
        }
    }
}