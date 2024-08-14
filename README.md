<h1> Kafka Messaging Project </h1>

<h3> Overview </h3>
<p> This project demonstrates how to use Kafka for messaging with an ASP.NET Core Web API and a background service. The project includes:
  <ul>
    <li>A Kafka producer implemented in an ASP.NET Core Web API controller to send messages to a Kafka topic.</li>
    <li>A background service that consumes messages from the Kafka topic continuously, starting from the earliest offset.</li>
  </ul>
</p>

<h3> Prerequisites </h3>
<p>Before running the project, ensure you have the following installed:
<ul>
  <li> [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)</li>
  <li> [Apache Kafka](https://kafka.apache.org/downloads) (ensure Kafka broker is running)</li>
</ul>
</p>

<h3> Setup Instructions </h3>
<h3> Kafka Broker Setup</h3>
<ul>
  <li> Download and install Apache Kafka from the [official site](https://kafka.apache.org/downloads).</li>
  <li> Start the Kafka broker and Zookeeper by running the following commands:</li>
</ul> <p></p>
<div class="codehilite">
<pre><code> 
    # Start ZooKeeper
      zookeeper-server-start.sh config/zookeeper.properties
    # Start Kafka Broker
      kafka-server-start.sh config/server.properties
</code></pre>
</div>

<h3>Project Setup</h3>
<p> Clone the repository: </p><p></p>
<div class="codehilite">
<pre><code> 
   git clone https://github.com/ayethandar07/Microservices-with-Dotnet-and-Apache-Kafka.git
</code></pre>
</div>
<p>Update the Kafka configuration in appsettings.json:</p><p></p>
<div class="codehilite">
<pre><code> 
   {
       "KafkaSettings": {
           "BootstrapServers": "localhost:9093",
           "Topic": "yourTopic",
           "GroupId": "YourGroupId",
           "Acks": "All",
           "AutoOffsetReset": "Earliest",
           "EnableAutoCommit": false
       }
  }
</code></pre>
</div>

<h3> Additional Information</h3>
<ul>
  <li>Kafka Documentation: Kafka Documentation</li>
  <li>ASP.NET Core Documentation: ASP.NET Core Documentation</li>
</ul>

<h3> Contact </h3>
<p>For questions or feedback, please contact <a href="mailto:athandar1998@gmail.com">athandar1998@gmail.com</a>.</p>
