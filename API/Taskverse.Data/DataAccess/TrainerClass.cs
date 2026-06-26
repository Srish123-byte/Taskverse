using System;
using System.Collections.Generic;
using System.Text;

namespace Taskverse.Data.DataAccess;

public class TrainerClass
{
    public Guid TrainerId { get; set; }
    public Guid ClassId { get; set; }

    // Navigation properties
    public Trainer Trainer { get; set; }
    public Class Class { get; set; }
}
