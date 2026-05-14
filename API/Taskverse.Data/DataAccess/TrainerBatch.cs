using System;
using System.Collections.Generic;
using System.Text;

namespace Taskverse.Data.DataAccess;

public class TrainerBatch
{
    public Guid TrainerId { get; set; }
    public Guid BatchId { get; set; }

    // Navigation properties
    public Trainer Trainer { get; set; }
    public Batch Batch { get; set; }
}
