using Gba_Domain.Models;
using Gba_Domain.Services;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Gba_Infra.Services;

public class GbaMatchKubernetesService : IGbaMatchKubernetesService
{
    private readonly Kubernetes _client;
    private readonly ILogger<GbaMatchKubernetesService> _logger;

    public GbaMatchKubernetesService(ILogger<GbaMatchKubernetesService> logger)
    {
        _logger = logger;
        var configPath = @"C:\Users\igor_\.kube\k3s.yaml";
        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(configPath);
        _client = new Kubernetes(config);

        _logger.LogInformation("Kubernetes client initialized for GBA Match");
    }

    public async Task<GbaMatchInfo> CreateMatchAsync(Guid matchId)
    {
        var podName = $"gba-match-{matchId:N}";
        var serviceName = $"svc-{podName}";
        var label = new Dictionary<string, string> { { "app", "gba-match" }, { "match-id", matchId.ToString() } };

        var portVideo = GetRandomPort();
        int portInput;
        do { portInput = GetRandomPort(); } while (portInput == portVideo);

        var pod = new V1Pod
        {
            Metadata = new V1ObjectMeta { Name = podName, Labels = label },
            Spec = new V1PodSpec
            {
                Containers = new List<V1Container>
                {
                    new()
                    {
                        Name = "gba-match",
                        Image = "igormendonca/gba-match:latest",
                        Ports = new List<V1ContainerPort>
                        {
                            new() { ContainerPort = 3000 },
                            new() { ContainerPort = 4000 }
                        },
                        Env = new List<V1EnvVar>
                        {
                            new() { Name = "MATCH_ID", Value = matchId.ToString() }
                        }
                    }
                },
                RestartPolicy = "Never"
            }
        };

        var service = new V1Service
        {
            Metadata = new V1ObjectMeta { Name = serviceName, Labels = label },
            Spec = new V1ServiceSpec
            {
                Type = "NodePort",
                Selector = label,
                Ports = new List<V1ServicePort>
                {
                    new() { Name = "video", Port = 3000, TargetPort = 3000, NodePort = portVideo },
                    new() { Name = "input", Port = 4000, TargetPort = 4000, NodePort = portInput }
                }
            }
        };

        try
        {
            await _client.CreateNamespacedServiceAsync(service, "default");
            await _client.CreateNamespacedPodAsync(pod, "default");

            _logger.LogInformation($"✅ Criado pod e serviço para match {matchId}");

            var nodeIP = await GetNodeIP();

            return new GbaMatchInfo
            {
                MatchId = matchId,
                PodName = podName,
                VideoUrl = $"ws://{nodeIP}:{portVideo}",
                InputUrl = $"ws://{nodeIP}:{portInput}",
                Status = "Running"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao criar pod/serviço para match {matchId}");

            try { await _client.DeleteNamespacedServiceAsync(serviceName, "default"); } catch { }
            throw;
        }
    }

    public async Task DeleteMatchAsync(string podName)
    {
        var serviceName = $"svc-{podName}";

        try
        {
            await _client.DeleteNamespacedPodAsync(podName, "default");
            _logger.LogInformation($"🗑️ Pod deletado: {podName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao deletar pod {podName}");
        }

        try
        {
            await _client.DeleteNamespacedServiceAsync(serviceName, "default");
            _logger.LogInformation($"🗑️ Serviço deletado: {serviceName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao deletar serviço {serviceName}");
        }
    }

    public async Task DeleteAllMatchesAsync()
    {
        try
        {
            // Lista todos os pods com a label app=gba-match
            var pods = await _client.ListNamespacedPodAsync("default", labelSelector: "app=gba-match");
            
            // Lista todos os serviços com a label app=gba-match
            var services = await _client.ListNamespacedServiceAsync("default", labelSelector: "app=gba-match");

            var deletedPods = 0;
            var deletedServices = 0;

            // Deleta todos os pods
            foreach (var pod in pods.Items)
            {
                try
                {
                    await _client.DeleteNamespacedPodAsync(pod.Metadata.Name, "default");
                    _logger.LogInformation($"🗑️ Pod deletado: {pod.Metadata.Name}");
                    deletedPods++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao deletar pod {pod.Metadata.Name}");
                }
            }

            // Deleta todos os serviços
            foreach (var service in services.Items)
            {
                try
                {
                    await _client.DeleteNamespacedServiceAsync(service.Metadata.Name, "default");
                    _logger.LogInformation($"🗑️ Serviço deletado: {service.Metadata.Name}");
                    deletedServices++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao deletar serviço {service.Metadata.Name}");
                }
            }

            _logger.LogInformation($"✅ Deletados {deletedPods} pods e {deletedServices} serviços");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar todas as matches");
            throw;
        }
    }

    private int GetRandomPort() => new Random().Next(30000, 32767);

    private async Task<string> GetNodeIP()
    {
        var nodes = await _client.ListNodeAsync();
        return nodes.Items
                   .SelectMany(n => n.Status.Addresses)
                   .FirstOrDefault(a => a.Type == "ExternalIP")?.Address
               ?? nodes.Items
                   .SelectMany(n => n.Status.Addresses)
                   .FirstOrDefault(a => a.Type == "InternalIP")?.Address
               ?? "127.0.0.1";
    }
}