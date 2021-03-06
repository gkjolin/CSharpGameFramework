﻿using System;
using System.Collections.Generic;
using System.IO;
using GameFramework;

namespace StorySystem
{
    public sealed class StoryConfigManager
    {
        public void LoadStories(int sceneId, string _namespace, params string[] files)
        {
            for (int i = 0; i < files.Length; i++) {
                LoadStory(files[i], sceneId, _namespace);
            }
        }
        public bool ExistStory(string storyId, int sceneId)
        {
            return null != GetStoryInstanceResource(storyId, sceneId);
        }
        public void LoadStory(string file, int sceneId, string _namespace)
        {
            if (!string.IsNullOrEmpty(file)) {
                Dsl.DslFile dataFile = new Dsl.DslFile();
#if DEBUG
                try {
                    if (dataFile.Load(file, LogSystem.Log)) {
                        Load(dataFile, sceneId, _namespace, file);
                    } else {
                        LogSystem.Error("LoadStory file:{0} scene:{1} failed", file, sceneId);
                    }
                } catch (Exception ex) {
                    LogSystem.Error("LoadStory file:{0} scene:{1} Exception:{2}\n{3}", file, sceneId, ex.Message, ex.StackTrace);
                }
#else
        try {
          dataFile.LoadBinaryFile(file, GlobalVariables.Instance.DecodeTable);
          Load(dataFile, sceneId, _namespace, file);
        } catch {
        }
#endif
            }
        }
        public void LoadStoryText(string text, int sceneId, string _namespace)
        {
#if DEBUG
            try {
                Dsl.DslFile dataFile = new Dsl.DslFile();
                if (dataFile.LoadFromString(text, "storytext", LogSystem.Log)) {
                    Load(dataFile, sceneId, _namespace, "storytext");
                } else {
                    LogSystem.Error("LoadStoryText text:{0} scene:{1} failed", text, sceneId);
                }
            } catch (Exception ex) {
                LogSystem.Error("LoadStoryText text:{0} scene:{1} Exception:{2}\n{3}", text, sceneId, ex.Message, ex.StackTrace);
            }
#else
      LoadStoryCode(text, sceneId, _namespace);
#endif
        }
        public void LoadStoryCode(string code, int sceneId, string _namespace)
        {
            try {
                Dsl.DslFile dataFile = new Dsl.DslFile();
                dataFile.LoadBinaryCode(code, GlobalVariables.Instance.DecodeTable);
                Load(dataFile, sceneId, _namespace, "storycode");
            } catch {
            }
        }

        public Dictionary<string, StoryInstance> GetStories(int sceneId)
        {
            Dictionary<string, StoryInstance> ret;
            m_StoryInstances.TryGetValue(sceneId, out ret);
            return ret;
        }
        public Dictionary<string, StoryInstance> GetStories(string file)
        {
            Dictionary<string, StoryInstance> ret;
            m_StoryInstancePool.TryGetValue(file, out ret);
            return ret;
        }
        public StoryInstance NewStoryInstance(string storyId, int sceneId)
        {
            StoryInstance instance = null;
            StoryInstance temp = GetStoryInstanceResource(storyId, sceneId);
            if (null != temp) {
                instance = temp.Clone();
            }
            return instance;
        }
        public void Clear()
        {
            lock (m_Lock) {
                m_StoryInstances.Clear();
                m_StoryInstancePool.Clear();
            }
        }

        private void Load(Dsl.DslFile dataFile, int sceneId, string _namespace, string resourceName)
        {
            lock (m_Lock) {
                Dictionary<string, StoryInstance> existStoryInstances;
                if (!m_StoryInstancePool.TryGetValue(resourceName, out existStoryInstances)) {
                    existStoryInstances = new Dictionary<string, StoryInstance>();
                    m_StoryInstancePool.Add(resourceName, existStoryInstances);

                    for (int i = 0; i < dataFile.DslInfos.Count; i++) {
                        if (dataFile.DslInfos[i].GetId() == "story" || dataFile.DslInfos[i].GetId() == "script") {
                            Dsl.FunctionData funcData = dataFile.DslInfos[i].First;
                            if (null != funcData) {
                                Dsl.CallData callData = funcData.Call;
                                if (null != callData && callData.HaveParam()) {
                                    StoryInstance instance = new StoryInstance();
                                    if (!string.IsNullOrEmpty(_namespace)) {
                                        instance.Namespace = _namespace;
                                    }
                                    instance.Init(dataFile.DslInfos[i]);
                                    string storyId;
                                    if (string.IsNullOrEmpty(_namespace)) {
                                        storyId = instance.StoryId;
                                    } else {
                                        storyId = string.Format("{0}:{1}", _namespace, instance.StoryId);
                                        instance.StoryId = storyId;
                                    }
                                    if (!existStoryInstances.ContainsKey(storyId)) {
                                        existStoryInstances.Add(storyId, instance);
                                    } else {
                                        existStoryInstances[storyId] = instance;
                                    }
                                    LogSystem.Info("ParseStory {0} {1}", storyId, sceneId);
                                }
                            }
                        }
                    }
                }
                Dictionary<string, StoryInstance> storyInstances;
                if (!m_StoryInstances.TryGetValue(sceneId, out storyInstances)) {
                    storyInstances = new Dictionary<string, StoryInstance>(existStoryInstances);
                    m_StoryInstances.Add(sceneId, storyInstances);
                } else {
                    foreach (var pair in existStoryInstances) {
                        if (!storyInstances.ContainsKey(pair.Key)) {
                            storyInstances.Add(pair.Key, pair.Value);
                        } else {
                            storyInstances[pair.Key] = pair.Value;
                        }
                    }
                }
            }
        }
        private StoryInstance GetStoryInstanceResource(string storyId, int sceneId)
        {
            StoryInstance instance = null;
            lock (m_Lock) {
                Dictionary<string, StoryInstance> storyInstances;
                if (m_StoryInstances.TryGetValue(sceneId, out storyInstances)) {
                    storyInstances.TryGetValue(storyId, out instance);
                }
            }
            return instance;
        }

        private StoryConfigManager() { }

        private object m_Lock = new object();
        private Dictionary<int, Dictionary<string, StoryInstance>> m_StoryInstances = new Dictionary<int, Dictionary<string, StoryInstance>>();
        private Dictionary<string, Dictionary<string, StoryInstance>> m_StoryInstancePool = new Dictionary<string, Dictionary<string, StoryInstance>>();

        public static StoryConfigManager NewInstance()
        {
            return new StoryConfigManager();
        }

        public static StoryConfigManager Instance
        {
            get { return s_Instance; }
        }
        private static StoryConfigManager s_Instance = new StoryConfigManager();
    }
}
