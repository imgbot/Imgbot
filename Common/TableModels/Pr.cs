using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.TableModels
{
    public class Pr : TableEntity
    {
        public Pr()
        {
        }

        public Pr(string owner)
        {
            // reverse chronological order
            RowKey = (new DateTime(3000, 1, 1).Ticks - DateTime.UtcNow.Ticks).ToString();
            PartitionKey = owner;
            Owner = owner;
        }

        public string RepoName { get; set; }

        public string Owner { get; set; }

        public long Id { get; set; }

        public int Number { get; set; }

        public int NumImages { get; set; }

        public double SizeBefore { get; set; }

        public double SizeAfter { get; set; }

        public double PercentReduced { get; set; }

        public double SpaceReduced => SizeBefore - SizeAfter;
    }
}
