﻿using System;

namespace NotesMarketPlace.Models.Admin
{
    public class DashboardModel
    {

        public int Id { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public float AttachmentSize { get; set; }

        public decimal? Price { get; set; }

        public string Publisher { get; set; }

        public DateTime PublishDate { get; set; }

        public int publishMonth { get; set; }

        public int? TotalDownloads { get; set; }

        public int userid { get; set; }

        public string filename { get; set; }

        public bool IsPaid { get; set; }

    }

    public class MonthModel
    {
        public int digit { get; set; }
        public string Month { get; set; }
    }
}
