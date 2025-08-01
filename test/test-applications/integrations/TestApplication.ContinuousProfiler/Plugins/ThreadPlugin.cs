// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

namespace TestApplication.ContinuousProfiler;

public class ThreadPlugin
{
    public Tuple<bool, uint, bool, uint, TimeSpan, TimeSpan, object> GetContinuousProfilerConfiguration()
    {
        var threadSamplingEnabled = true;
        var threadSamplingInterval = 1000u;
        var allocationSamplingEnabled = false;
        var maxMemorySamplesPerMinute = 200u;
        var exportInterval = TimeSpan.FromMilliseconds(500);
        var exportTimeout = TimeSpan.FromMilliseconds(5000);
        object continuousProfilerExporter = new OtlpOverHttpExporter(TimeSpan.FromMilliseconds(threadSamplingInterval), new SampleNativeFormatParser());

        return Tuple.Create(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, exportTimeout, continuousProfilerExporter);
    }

    public ResourceBuilder ConfigureResource(ResourceBuilder builder)
    {
        // TODO hack to fetch resources from the SDK and store them to be able to sent to OTLP Profiling
        ResourcesProvider.Configure(builder);
        return builder;
    }
}
