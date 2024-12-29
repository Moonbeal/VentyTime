using FluentValidation;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Validators
{
    public class EventValidator : AbstractValidator<Event>
    {
        public EventValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(100)
                .WithMessage("Title must not exceed 100 characters");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MinimumLength(10)
                .WithMessage("Description must be at least 10 characters")
                .MaximumLength(1000)
                .WithMessage("Description must not exceed 1000 characters");

            RuleFor(x => x.Location)
                .NotEmpty()
                .WithMessage("Location is required")
                .MaximumLength(200)
                .WithMessage("Location must not exceed 200 characters");

            RuleFor(x => x.MaxAttendees)
                .GreaterThan(0)
                .WithMessage("Maximum attendees must be greater than 0");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Price must be greater than or equal to 0");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required")
                .Must(date => date > DateTime.UtcNow)
                .WithMessage("Start date must be in the future");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("End date is required")
                .Must((evt, endDate) => endDate > evt.StartDate)
                .WithMessage("End date must be after start date");

            RuleFor(x => x.Category)
                .NotEmpty()
                .WithMessage("Category is required");

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Invalid event type");
        }
    }
}
