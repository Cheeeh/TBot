using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBot.Ogame.Infrastructure.Enums;

namespace TBot.Ogame.Infrastructure.Models {
	/// <summary>
	/// Celestial under consideration to be targetted for farming.
	/// </summary>
	public class DiscoveryBlackList {
		public DiscoveryBlackList(Coordinate coordinate, DateTime dateTime) {
			Coordinate = coordinate;
            DateTime = dateTime;
		}
		public Coordinate Coordinate { get; set; }
		public DateTime DateTime { get; set; }
	}

}
