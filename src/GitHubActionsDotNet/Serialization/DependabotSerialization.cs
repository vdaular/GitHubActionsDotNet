﻿using GitHubActionsDotNet.Common;
using GitHubActionsDotNet.Models.Dependabot;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GitHubActionsDotNet.Serialization
{
    public static class DependabotSerialization
    {
        public static string Serialize(string startingDirectory,
            List<string> files,
            string interval = null,
            string time = null,
            string timezone = null,
            List<string> assignees = null,
            int openPRLimit = 0,
            bool includeActions = true)
        {
            if (startingDirectory == null)
            {
                return "";
            }

            DependabotRoot root = new DependabotRoot();
            List<Package> packages = new List<Package>();
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                string cleanedFilePath = file.Replace(startingDirectory + "/", "");
                cleanedFilePath = cleanedFilePath.Replace(startingDirectory + "\\", "");
                cleanedFilePath = cleanedFilePath.Replace(fileInfo.Name, "");
                cleanedFilePath = "/" + cleanedFilePath.Replace("\\", "/");
                string packageEcoSystem = DependabotCommon.GetPackageEcoSystemFromFileName(fileInfo.Name);
                Package package = DependabotPackageSerialization.CreatePackage(cleanedFilePath, packageEcoSystem, interval, time, timezone, assignees, openPRLimit);
                packages.Add(package);
            }
            //Add actions
            if (includeActions == true)
            {
                Package actionsPackage = DependabotPackageSerialization.CreatePackage("/", "github-actions", interval, time, timezone, assignees, openPRLimit);
                packages.Add(actionsPackage);
            }
            root.updates = packages;

            //Serialize the object into YAML
            string yaml = YamlSerialization.SerializeYaml(root);

            //I can't use - in variable names, so replace _ with -
            yaml = yaml.Replace("package_ecosystem", "package-ecosystem");
            yaml = yaml.Replace("open_pull_requests_limit", "open-pull-requests-limit");
            yaml = yaml.Replace("replaces_base", "replaces-base");
            yaml = yaml.Replace("dependency_name", "dependency-name");
            yaml = yaml.Replace("dependency_type", "dependency-type");
            yaml = yaml.Replace("prefix_development", "prefix-development");
            yaml = yaml.Replace("commit_message", "commit-message");
            yaml = yaml.Replace("update_types", "update-types");
            yaml = yaml.Replace("insecure_external_code_execution", "insecure-external-code-execution");
            yaml = yaml.Replace("pull_request_branch_name", "pull-request-branch-name");
            yaml = yaml.Replace("rebase_strategy", "rebase-strategy");
            yaml = yaml.Replace("target_branch", "target-branch");
            yaml = yaml.Replace("versioning_strategy", "versioning-strategy");

            return yaml;
        }

        public static DependabotRoot Deserialize(string yaml)
        {
            yaml = yaml.Replace("package-ecosystem", "package_ecosystem");
            yaml = yaml.Replace("open-pull-requests-limit", "open_pull_requests_limit");
            yaml = yaml.Replace("replaces-base", "replaces_base");
            yaml = yaml.Replace("dependency-name", "dependency_name");
            yaml = yaml.Replace("dependency-type", "dependency_type");
            yaml = yaml.Replace("prefix-development", "prefix_development");
            yaml = yaml.Replace("commit-message", "commit_message");
            yaml = yaml.Replace("update-types", "update_types");
            yaml = yaml.Replace("insecure-external-code-execution", "insecure_external_code_execution");
            yaml = yaml.Replace("pull-request-branch-name", "pull_request_branch_name");
            yaml = yaml.Replace("rebase-strategy", "rebase_strategy");
            yaml = yaml.Replace("target-branch", "target_branch");
            yaml = yaml.Replace("versioning-strategy", "versioning_strategy");

            //DependabotRoot root = YamlSerialization.DeserializeYaml<DependabotRoot>(yaml);
            //convert the yaml into json, it's easier to parse
            JsonElement jsonObject = new JsonElement();
            if (yaml != null)
            {
                jsonObject = JsonSerialization.DeserializeStringToJsonElement(yaml);
            }

            //Build up the GitHub object piece by piece
            DependabotRoot root = new DependabotRoot();

            if (jsonObject.ValueKind != JsonValueKind.Undefined)
            {
                //Version
                if (jsonObject.TryGetProperty("name", out JsonElement jsonElement))
                {
                    root.version = jsonElement.ToString().Replace("version:", "").Replace(System.Environment.NewLine, "").Trim();
                }

                //Registries
                if (jsonObject.TryGetProperty("registries", out jsonElement))
                {
                    root.registries = YamlSerialization.DeserializeYaml<IDictionary<string, Registry>>(jsonElement.ToString()); //JsonSerialization.(jsonElement.ToString());
                }

                //Packages
                if (jsonObject.TryGetProperty("updates", out jsonElement))
                {
                    foreach (JsonElement packagesItem in jsonElement.EnumerateArray())
                    {
                        string packageYaml = packagesItem.ToString();
                        if (root.updates == null)
                        {
                            root.updates = new List<Package>();
                        }
                        root.updates.Add(ProcessPackage(packageYaml));
                    }
                }
            }

            return root;
        }

        private static Package ProcessPackage(string packageYaml)
        {
            Package package = null;
            if (packageYaml != null)
            {
                //Try the string[] variable first - I think that will be the most common
                try
                {
                    package = YamlSerialization.DeserializeYaml<PackageStringArray>(packageYaml);
                }
                catch
                {
                    //If it didn't work, try the simple string one, the next most common
                    package = YamlSerialization.DeserializeYaml<PackageString>(packageYaml);
                }
            }
            return package;
        }
    }
}
