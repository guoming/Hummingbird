using Hummingbird.Extensions.UidGenerator.Abastracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Hummingbird.Extensions.UidGenerator.Implements
{
    class WorkIdCreateStrategyBuilder : IWorkIdCreateStrategyBuilder
    {
        private readonly IServiceCollection _serviceDescriptors;

        public WorkIdCreateStrategyBuilder(IServiceCollection serviceDescriptors)
        {
            this._serviceDescriptors = serviceDescriptors;
        }

        public IServiceCollection Services { get { return _serviceDescriptors; } }

        public int CenterId { get; set; }
    }
}