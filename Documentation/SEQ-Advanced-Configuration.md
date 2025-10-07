# LogCtx Advanced SEQ Configuration Guide

**Production-ready SEQ configurations, queries, and dashboard patterns for LogCtx - version 0.3.1**

This guide consolidates all SEQ configuration patterns, eliminating duplicates across documentation and providing production-ready solutions.

---

## 🎯 **Master SEQ Configuration Repository**

This file serves as the **single source of truth** for all SEQ configurations. Other documentation files should reference this guide instead of duplicating SEQ snippets.

---

## 📊 **SEQ Target Configurations**

### **1. Basic Development SEQ Target**

```xml
<!-- ✅ BASIC DEV CONFIG - Copy this for simple local development -->
<target xsi:type="Seq" name="seq" 
        serverUrl="http://localhost:5341" 
        apiKey="">
  <property name="Application" value="YourAppName" />
  <property name="Environment" value="Development" />
  <property name="MachineName" value="${machinename}" />
  <property name="ProcessId" value="${processid}" />
</target>
```

### **2. Production SEQ Target with Security**

```xml
<!-- ✅ PRODUCTION CONFIG - Copy this for production deployments -->
<target xsi:type="Seq" name="seq" 
        serverUrl="https://seq.yourcompany.com" 
        apiKey="${environment:SEQ_API_KEY}"
        batchPostingLimit="50"
        queueSizeLimit="100000"
        serverTimeout="00:00:30"
        retryDelay="00:00:01"
        retryLimit="3">
  
  <!-- Static application properties -->
  <property name="Application" value="YourAppName" />
  <property name="Environment" value="${environment:ENVIRONMENT:whenEmpty=Production}" />
  <property name="Server" value="${machinename}" />
  <property name="Version" value="${assembly-version}" />
  <property name="BuildNumber" value="${environment:BUILD_NUMBER}" />
  
  <!-- Performance monitoring -->
  <property name="ProcessId" value="${processid}" />
  <property name="ThreadId" value="${threadid}" />
  <property name="MemoryUsage" value="${gc:gen=0}" />
  
  <!-- Deployment context -->
  <property name="DeploymentSlot" value="${environment:DEPLOYMENT_SLOT}" />
  <property name="Region" value="${environment:AZURE_REGION}" />
</target>
```

### **3. High-Volume SEQ Target with Batching**

```xml
<!-- ✅ HIGH-VOLUME CONFIG - Copy this for high-throughput applications -->
<target xsi:type="Seq" name="seq"
        serverUrl="http://seq.internal:5341"
        apiKey="${environment:SEQ_API_KEY}"
        batchPostingLimit="1000"
        period="00:00:05"
        queueSizeLimit="1000000"
        serverTimeout="00:01:00"
        retryDelay="00:00:02"
        retryLimit="5">
  
  <!-- Minimal properties for performance -->
  <property name="Application" value="HighVolumeApp" />
  <property name="Environment" value="Production" />
  <property name="Instance" value="${environment:INSTANCE_ID}" />
  
  <!-- Conditional enrichment - only for structured events -->
  <when condition="length('${event-properties:ServiceName}') > 0">
    <property name="HasStructuredContext" value="true" />
    <property name="ContextSource" value="LogCtx" />
  </when>
</target>
```

### **4. Multi-Environment SEQ Target**

```xml
<!-- ✅ MULTI-ENV CONFIG - Copy this for flexible environment support -->
<target xsi:type="Seq" name="seq"
        serverUrl="${environment:SEQ_URL:whenEmpty=http://localhost:5341}"
        apiKey="${environment:SEQ_API_KEY}"
        batchPostingLimit="${environment:SEQ_BATCH_SIZE:whenEmpty=100}"
        queueSizeLimit="${environment:SEQ_QUEUE_SIZE:whenEmpty=10000}">
  
  <!-- Environment-aware properties -->
  <property name="Application" value="YourApp" />
  <property name="Environment" value="${environment:ENVIRONMENT:whenEmpty=Development}" />
  <property name="ConfigSource" value="Environment" />
  
  <!-- Conditional development properties -->
  <when condition="'${environment:ENVIRONMENT}' == 'Development'">
    <property name="DeveloperMachine" value="${machinename}" />
    <property name="DebugMode" value="true" />
  </when>
  
  <!-- Conditional production properties -->
  <when condition="'${environment:ENVIRONMENT}' == 'Production'">
    <property name="Cluster" value="${environment:CLUSTER_NAME}" />
    <property name="ServiceTier" value="${environment:SERVICE_TIER}" />
    <property name="MonitoringEnabled" value="true" />
  </when>
</target>
```

---

## 🔧 **SEQ Rules and Filtering**

### **1. Structured Events Only Rule**

```xml
<!-- ✅ STRUCTURED ONLY - Only log events with LogCtx context -->
<rules>
  <!-- Only send events with ServiceName property (LogCtx events) -->
  <logger name="*" minlevel="Information" writeTo="seq">
    <filters defaultAction="Log">
      <when condition="length('${event-properties:ServiceName}') == 0" action="Ignore" />
    </filters>
  </logger>
</rules>
```

### **2. Performance-Filtered Rule**

```xml
<!-- ✅ PERFORMANCE FILTERED - Reduce noise in production -->
<rules>
  <!-- Different levels for different loggers -->
  <logger name="Microsoft.*" minlevel="Warning" writeTo="seq" final="true" />
  <logger name="System.*" minlevel="Warning" writeTo="seq" final="true" />
  <logger name="YourApp.*" minlevel="Information" writeTo="seq" final="true" />
  
  <!-- Everything else at Error level -->
  <logger name="*" minlevel="Error" writeTo="seq" />
</rules>
```

### **3. Environment-Specific Rules**

```xml
<!-- ✅ ENVIRONMENT RULES - Different behavior per environment -->
<rules>
  <!-- Development: Everything to SEQ -->
  <when condition="'${environment:ENVIRONMENT}' == 'Development'">
    <logger name="*" minlevel="Debug" writeTo="seq" />
  </when>
  
  <!-- Staging: Info and above -->
  <when condition="'${environment:ENVIRONMENT}' == 'Staging'">
    <logger name="*" minlevel="Information" writeTo="seq" />
  </when>
  
  <!-- Production: Warning and above, except for our app -->
  <when condition="'${environment:ENVIRONMENT}' == 'Production'">
    <logger name="YourApp.*" minlevel="Information" writeTo="seq" final="true" />
    <logger name="*" minlevel="Warning" writeTo="seq" />
  </when>
</rules>
```

---

## 📋 **SEQ Dashboard Queries**

### **1. LogCtx Context Analysis Queries**

```sql
-- ✅ OPERATIONS OVERVIEW - See all operations with timing
select ServiceName, Operation, count(*) as Count, 
       avg(DurationMs) as AvgDuration, 
       max(DurationMs) as MaxDuration
from stream 
where ServiceName is not null and Operation is not null
group by ServiceName, Operation
order by Count desc

-- ✅ ERROR ANALYSIS - Group errors by type and service
select ServiceName, Operation, ErrorType, count(*) as ErrorCount,
       max(@Timestamp) as LastOccurrence
from stream 
where @Level = 'Error' and ErrorType is not null
group by ServiceName, Operation, ErrorType
order by ErrorCount desc

-- ✅ PERFORMANCE HOTSPOTS - Find slow operations
select ServiceName, Operation, 
       avg(DurationMs) as AvgDuration,
       count(*) as CallCount
from stream 
where DurationMs > 1000 and ServiceName is not null
group by ServiceName, Operation
order by AvgDuration desc

-- ✅ USER ACTIVITY - Track user-specific operations
select UserId, ServiceName, Operation, count(*) as ActivityCount,
       min(@Timestamp) as FirstActivity,
       max(@Timestamp) as LastActivity  
from stream 
where UserId is not null
group by UserId, ServiceName, Operation
order by ActivityCount desc
```

### **2. Application Health Queries**

```sql
-- ✅ ERROR RATE - Monitor application error trends
select datebucket(@Timestamp, '1h') as Hour,
       count(*) as TotalEvents,
       count(case when @Level = 'Error' then 1 end) as Errors,
       count(case when @Level = 'Warning' then 1 end) as Warnings
from stream 
where @Timestamp >= now() - 24h
group by datebucket(@Timestamp, '1h')
order by Hour desc

-- ✅ THROUGHPUT ANALYSIS - Monitor operation volume
select datebucket(@Timestamp, '5m') as TimeWindow,
       ServiceName,
       count(*) as OperationCount
from stream 
where ServiceName is not null and @Timestamp >= now() - 2h
group by datebucket(@Timestamp, '5m'), ServiceName
order by TimeWindow desc

-- ✅ INITIALIZATION TRACKING - Monitor app startups
select @Timestamp, ApplicationName, Version, Environment, MachineName
from stream 
where @MessageTemplate like '%Application%start%'
order by @Timestamp desc

-- ✅ CONTEXT QUALITY - Monitor LogCtx usage
select datebucket(@Timestamp, '1h') as Hour,
       count(*) as TotalEvents,
       count(case when ServiceName is not null then 1 end) as WithContext,
       round(count(case when ServiceName is not null then 1 end) * 100.0 / count(*), 2) as ContextPercentage
from stream 
where @Timestamp >= now() - 24h
group by datebucket(@Timestamp, '1h')
order by Hour desc
```

### **3. Business Intelligence Queries**

```sql
-- ✅ USER JOURNEY - Track user operations in sequence
select @Timestamp, UserId, ServiceName, Operation, RequestId
from stream 
where UserId = 'specific-user-id'
order by @Timestamp desc
limit 100

-- ✅ FEATURE USAGE - Monitor feature adoption
select Operation, count(*) as UsageCount,
       count(distinct UserId) as UniqueUsers,
       min(@Timestamp) as FirstUsed,
       max(@Timestamp) as LastUsed
from stream 
where ServiceName = 'FeatureService'
group by Operation
order by UsageCount desc

-- ✅ PERFORMANCE BY USER - Identify problematic user patterns
select UserId, avg(DurationMs) as AvgDuration,
       count(*) as OperationCount,
       count(case when @Level = 'Error' then 1 end) as ErrorCount
from stream 
where DurationMs is not null and UserId is not null
group by UserId
having count(*) > 10
order by AvgDuration desc
```

---

## 🎛️ **SEQ Dashboard Templates**

### **1. Application Overview Dashboard**

```yaml
# ✅ APP OVERVIEW - Copy this dashboard configuration
dashboard:
  name: "LogCtx Application Overview"
  refresh: "30s"
  charts:
    - title: "Operations per Minute"
      query: |
        select datebucket(@Timestamp, '1m') as Minute, count(*) as Operations
        from stream 
        where ServiceName is not null and @Timestamp >= now() - 1h
        group by datebucket(@Timestamp, '1m')
        order by Minute desc
      type: "timeseries"
      
    - title: "Error Rate"
      query: |
        select @Level, count(*) as Count
        from stream 
        where @Timestamp >= now() - 1h
        group by @Level
      type: "pie"
      
    - title: "Top Services"
      query: |
        select ServiceName, count(*) as Operations
        from stream 
        where ServiceName is not null and @Timestamp >= now() - 1h
        group by ServiceName
        order by Operations desc
        limit 10
      type: "bar"
      
    - title: "Slow Operations"
      query: |
        select ServiceName, Operation, max(DurationMs) as MaxDuration
        from stream 
        where DurationMs > 1000 and @Timestamp >= now() - 1h
        group by ServiceName, Operation
        order by MaxDuration desc
        limit 10
      type: "table"
```

### **2. Performance Monitoring Dashboard**

```yaml
# ✅ PERFORMANCE MONITOR - Copy this for performance tracking
dashboard:
  name: "LogCtx Performance Monitor"
  refresh: "15s"
  charts:
    - title: "Average Response Time"
      query: |
        select datebucket(@Timestamp, '2m') as Time, 
               avg(DurationMs) as AvgResponse
        from stream 
        where DurationMs is not null and @Timestamp >= now() - 30m
        group by datebucket(@Timestamp, '2m')
        order by Time desc
      type: "timeseries"
      
    - title: "95th Percentile Response Time"
      query: |
        select datebucket(@Timestamp, '5m') as Time,
               percentile(DurationMs, 95) as P95Response
        from stream 
        where DurationMs is not null and @Timestamp >= now() - 2h
        group by datebucket(@Timestamp, '5m')
        order by Time desc
      type: "timeseries"
      
    - title: "Throughput by Service"
      query: |
        select ServiceName, 
               count(*) / 60.0 as OperationsPerSecond
        from stream 
        where ServiceName is not null and @Timestamp >= now() - 1m
        group by ServiceName
        order by OperationsPerSecond desc
      type: "bar"
```

---

## 🔒 **Security and Compliance Configurations**

### **1. PII-Safe SEQ Configuration**

```xml
<!-- ✅ PII-SAFE CONFIG - Excludes sensitive data -->
<target xsi:type="Seq" name="seq"
        serverUrl="https://seq.company.com"
        apiKey="${environment:SEQ_API_KEY}">
  
  <!-- Standard non-sensitive properties -->
  <property name="Application" value="YourApp" />
  <property name="Environment" value="Production" />
  <property name="RequestType" value="${event-properties:Operation}" />
  
  <!-- Explicitly exclude PII fields -->
  <property name="DataPrivacy" value="PII-Filtered" />
  
  <!-- Only include hash of user ID, not actual ID -->
  <property name="UserHash" value="${sha256:${event-properties:UserId}}" />
</target>
```

### **2. Compliance-Ready Configuration**

```xml
<!-- ✅ COMPLIANCE CONFIG - Audit trail ready -->
<target xsi:type="Seq" name="seq"
        serverUrl="https://audit-seq.company.com"
        apiKey="${environment:AUDIT_SEQ_KEY}">
  
  <!-- Audit trail properties -->
  <property name="Application" value="YourApp" />
  <property name="Environment" value="Production" />
  <property name="AuditCategory" value="ApplicationActivity" />
  <property name="DataClassification" value="Internal" />
  <property name="RetentionPeriod" value="7Years" />
  
  <!-- Compliance metadata -->
  <property name="Regulation" value="GDPR" />
  <property name="DataController" value="YourCompany" />
  <property name="ProcessingPurpose" value="ApplicationMonitoring" />
</target>
```

---

## 🚀 **Performance Optimization**

### **1. High-Throughput Configuration**

```xml
<!-- ✅ HIGH THROUGHPUT - Optimized for volume -->
<target xsi:type="Seq" name="seq"
        serverUrl="http://seq.internal:5341"
        apiKey="${environment:SEQ_API_KEY}"
        batchPostingLimit="2000"
        period="00:00:02"
        queueSizeLimit="2000000"
        serverTimeout="00:02:00">
  
  <!-- Minimal properties for speed -->
  <property name="App" value="HighVolApp" />
  <property name="Env" value="Prod" />
  
  <!-- Async processing indicator -->
  <property name="ProcessingMode" value="HighThroughput" />
</target>
```

### **2. Memory-Efficient Configuration**

```xml
<!-- ✅ MEMORY EFFICIENT - Reduced memory footprint -->
<target xsi:type="Seq" name="seq"
        serverUrl="http://seq.local:5341"
        batchPostingLimit="100"
        queueSizeLimit="1000">
  
  <!-- Essential properties only -->
  <property name="Application" value="LowMemApp" />
  <property name="ProcessingMode" value="MemoryOptimized" />
</target>
```

---

## 📊 **Monitoring and Alerting**

### **1. SEQ Alert Queries**

```sql
-- ✅ HIGH ERROR RATE ALERT
select count(*) as ErrorCount
from stream 
where @Level = 'Error' and @Timestamp >= now() - 5m
having count(*) > 10

-- ✅ SLOW OPERATION ALERT  
select ServiceName, Operation, max(DurationMs) as MaxDuration
from stream 
where DurationMs > 5000 and @Timestamp >= now() - 5m
group by ServiceName, Operation
having max(DurationMs) > 5000

-- ✅ APPLICATION NOT RESPONDING ALERT
select count(*) as EventCount
from stream 
where ApplicationName = 'YourApp' and @Timestamp >= now() - 5m
having count(*) = 0

-- ✅ MEMORY PRESSURE ALERT
select avg(cast(MemoryUsage as real)) as AvgMemory
from stream 
where MemoryUsage is not null and @Timestamp >= now() - 5m
having avg(cast(MemoryUsage as real)) > 800000000
```

---

## 🔗 **Integration with Other Systems**

### **1. Elasticsearch Integration**

```xml
<!-- ✅ DUAL LOGGING - SEQ + Elasticsearch -->
<target xsi:type="Seq" name="seq" serverUrl="http://seq:5341">
  <property name="LogSink" value="SEQ" />
  <property name="Application" value="YourApp" />
</target>

<target xsi:type="ElasticSearch" name="elastic" 
        uri="http://elasticsearch:9200"
        index="logctx-${date:format=yyyy.MM.dd}">
  <property name="LogSink" value="Elasticsearch" />
  <property name="Application" value="YourApp" />
</target>

<rules>
  <logger name="*" minlevel="Information" writeTo="seq,elastic" />
</rules>
```

### **2. Application Insights Integration**

```xml
<!-- ✅ AZURE INTEGRATION - SEQ + App Insights -->
<target xsi:type="Seq" name="seq" serverUrl="http://seq:5341">
  <property name="LogSink" value="SEQ" />
</target>

<target xsi:type="ApplicationInsightsTarget" name="appInsights"
        instrumentationKey="${environment:APPINSIGHTS_KEY}">
  <property name="LogSink" value="ApplicationInsights" />
</target>

<rules>
  <logger name="*" minlevel="Information" writeTo="seq" />
  <logger name="*" minlevel="Warning" writeTo="appInsights" />
</rules>
```

---

## 📋 **Reference Documentation**

For basic SEQ setup, see [Step-0-Integration-Guide-CORRECTED.md].  
For configuration templates, see [Config-Snippets-CORRECTED.md].  
For LogCtx usage patterns, see [Usage-Patterns-Examples-CORRECTED.md].

---

## 🎯 **Configuration Selection Guide**

| **Use Case** | **Configuration** | **Best For** |
|--------------|-------------------|--------------|
| **Local Development** | Basic Development | Simple local testing |
| **Production Apps** | Production with Security | Enterprise applications |
| **High Volume** | High-Volume Batching | >1000 logs/second |
| **Multi-Environment** | Multi-Environment | CI/CD pipelines |
| **Compliance** | Compliance-Ready | Regulated industries |
| **Performance Critical** | High-Throughput | Latency-sensitive apps |

---

**Version:** 0.3.1  
**Last Updated:** October 2025  
**Usage:** This is the MASTER SEQ configuration source - reference from other docs