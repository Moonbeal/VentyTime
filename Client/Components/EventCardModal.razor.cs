using Microsoft.AspNetCore.Components;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Components
{
    public partial class EventCardModal : ComponentBase
    {
        [Parameter]
        public Event Event { get; set; } = default!;

        [Parameter]
        public EventCallback OnClose { get; set; }

        [Parameter]
        public EventCallback<int> OnViewDetails { get; set; }

        [Parameter]
        public EventCallback<int> OnRegister { get; set; }
    }
}
