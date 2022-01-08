﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDTools.Database {
    public static class Tiers {
        public static Tier Free { get { return new Tier(100, int.MaxValue); } }
        public static Tier VIP { get { return new Tier(500, int.MaxValue); } }

        /// <summary>
        /// Accounts with access to admin interface.
        /// </summary>
        public static Tier Admin { get { return new Tier(int.MaxValue, int.MaxValue); } }
    }
    public class Tier {
        /// <summary>
        /// The amount of likes left for the account.
        /// </summary>
        public int LikesLeft { get; set; }
        public int MaxAccountDays { get; set; }

        public Tier(int maxLikes, int maxDays) {
            LikesLeft = maxLikes;
            MaxAccountDays = maxDays;
        }
    }
}
