﻿using BoardApi.Hubs;
using Domains.Epics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BoardApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EpicsController : ControllerBase
    {
        private readonly EpicsDomain domain;
        private readonly IHubContext<DocumentHub> _hubContext;

        public EpicsController(IHubContext<DocumentHub> hubContext)
        {
            domain = new EpicsDomain();
            _hubContext = hubContext;
        }

        [HttpGet("GetAll")]
        public IEnumerable<Epic> GetAll()
        {
            return domain.GetAll();
        }

        [HttpPut]
        public async Task<IEnumerable<Epic>> UpdateEpic(EpicUpdate update)
        {
            var epics = domain.Update(update.Epic);

            // Everyone except the sender
            await _hubContext.Clients.AllExcept(update.ConnectionId).SendAsync("UpdateEpic", update.Epic, update.OldStatus);

            // Everyone
            //await _hubContext.Clients.All.SendAsync("UpdateEpic", update.Epic, update.OldStatus);

            // If group is QA send alert
            if (update.Epic.Status == Domains.Enums.Status.Done)
            {
                await _hubContext.Clients.Group("QA").SendAsync("DevDone", update.Epic.Id.ToString());
            }
            return epics;
        }

        public class EpicUpdate
        {
            public Epic Epic { get; set; }
            public int OldStatus { get; set; }
            public string ConnectionId { get; set; }
        }
    }
}
