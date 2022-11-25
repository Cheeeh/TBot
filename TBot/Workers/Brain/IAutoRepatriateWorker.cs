using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tbot.Workers.Brain {
	public interface IAutoRepatriateWorker {
		Task Collect();
		Task CollectDeut(long minAmount = 0);
	}
}
