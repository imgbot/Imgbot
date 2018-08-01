using System;
using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp;

namespace Test
{
    public class SimpleCommitLog : IQueryableCommitLog
    {
        private readonly IEnumerable<Commit> _commits;

        public SimpleCommitLog(IEnumerable<Commit> commits)
        {
            _commits = commits;
        }

        public CommitSortStrategies SortedBy => throw new NotImplementedException();

        public IEnumerator<Commit> GetEnumerator() => _commits.GetEnumerator();

        public ICommitLog QueryBy(CommitFilter filter) => throw new NotImplementedException();

        public IEnumerable<LogEntry> QueryBy(string path) => throw new NotImplementedException();

        public IEnumerable<LogEntry> QueryBy(string path, CommitFilter filter) => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => _commits.GetEnumerator();
    }
}
