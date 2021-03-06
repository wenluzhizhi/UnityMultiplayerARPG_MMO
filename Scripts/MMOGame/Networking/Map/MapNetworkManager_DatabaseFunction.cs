﻿using LiteNetLib;
using LiteNetLibManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG.MMO
{
    public partial class MapNetworkManager
    {
        private IEnumerator LoadStorageRoutine(StorageId storageId)
        {
            if (!loadingStorageIds.Contains(storageId))
            {
                loadingStorageIds.Add(storageId);
                ReadStorageItemsJob job = new ReadStorageItemsJob(Database, storageId.storageType, storageId.storageOwnerId);
                job.Start();
                yield return StartCoroutine(job.WaitFor());
                if (job.result != null)
                    storageItems[storageId] = job.result;
                else
                    storageItems[storageId] = new List<CharacterItem>();
                loadingStorageIds.Remove(storageId);
            }
        }

        private IEnumerator LoadPartyRoutine(int id)
        {
            if (id > 0 && !loadingPartyIds.Contains(id))
            {
                loadingPartyIds.Add(id);
                ReadPartyJob job = new ReadPartyJob(Database, id);
                job.Start();
                yield return StartCoroutine(job.WaitFor());
                if (job.result != null)
                    parties[id] = job.result;
                else
                    parties.Remove(id);
                loadingPartyIds.Remove(id);
            }
        }

        private IEnumerator LoadGuildRoutine(int id)
        {
            if (id > 0 && !loadingGuildIds.Contains(id))
            {
                loadingGuildIds.Add(id);
                ReadGuildJob job = new ReadGuildJob(Database, id, CurrentGameInstance.SocialSystemSetting.GuildMemberRoles);
                job.Start();
                yield return StartCoroutine(job.WaitFor());
                if (job.result != null)
                    guilds[id] = job.result;
                else
                    guilds.Remove(id);
                loadingGuildIds.Remove(id);
            }
        }

        private IEnumerator SaveCharacterRoutine(IPlayerCharacterData playerCharacterData, string userId)
        {
            if (playerCharacterData != null && !savingCharacters.Contains(playerCharacterData.Id))
            {
                savingCharacters.Add(playerCharacterData.Id);
                UpdateCharacterJob job = new UpdateCharacterJob(Database, playerCharacterData);
                job.Start();
                yield return StartCoroutine(job.WaitFor());
                StorageId storageId = new StorageId(StorageType.Player, userId);
                if (storageItems.ContainsKey(storageId))
                {
                    UpdateStorageItemsJob updateStorageItemsJob = new UpdateStorageItemsJob(Database, storageId.storageType, storageId.storageOwnerId, storageItems[storageId]);
                    updateStorageItemsJob.Start();
                    yield return StartCoroutine(updateStorageItemsJob.WaitFor());
                }
                savingCharacters.Remove(playerCharacterData.Id);
                if (LogInfo)
                    Debug.Log("Character [" + playerCharacterData.Id + "] Saved");
            }
        }

        private IEnumerator SaveCharactersRoutine()
        {
            if (savingCharacters.Count == 0)
            {
                int i = 0;
                foreach (BasePlayerCharacterEntity playerCharacter in playerCharacters.Values)
                {
                    StartCoroutine(SaveCharacterRoutine(playerCharacter.CloneTo(new PlayerCharacterData()), playerCharacter.UserId));
                    ++i;
                }
                while (savingCharacters.Count > 0)
                {
                    yield return 0;
                }
                if (LogInfo)
                    Debug.Log("Saved " + i + " character(s)");
            }
        }

        private IEnumerator SaveBuildingRoutine(IBuildingSaveData buildingSaveData)
        {
            if (buildingSaveData != null && !savingBuildings.Contains(buildingSaveData.Id))
            {
                savingBuildings.Add(buildingSaveData.Id);
                UpdateBuildingJob job = new UpdateBuildingJob(Database, Assets.onlineScene.SceneName, buildingSaveData);
                job.Start();
                yield return StartCoroutine(job.WaitFor());
                StorageId storageId = new StorageId(StorageType.Building, buildingSaveData.Id);
                if (storageItems.ContainsKey(storageId))
                {
                    UpdateStorageItemsJob updateStorageItemsJob = new UpdateStorageItemsJob(Database, storageId.storageType, storageId.storageOwnerId, storageItems[storageId]);
                    updateStorageItemsJob.Start();
                    yield return StartCoroutine(updateStorageItemsJob.WaitFor());
                }
                savingBuildings.Remove(buildingSaveData.Id);
                if (LogInfo)
                    Debug.Log("Building [" + buildingSaveData.Id + "] Saved");
            }
        }

        private IEnumerator SaveBuildingsRoutine()
        {
            if (savingBuildings.Count == 0)
            {
                int i = 0;
                foreach (BuildingEntity buildingEntity in buildingEntities.Values)
                {
                    if (buildingEntity == null) continue;
                    StartCoroutine(SaveBuildingRoutine(buildingEntity.CloneTo(new BuildingSaveData())));
                    ++i;
                }
                while (savingBuildings.Count > 0)
                {
                    yield return 0;
                }
                if (LogInfo)
                    Debug.Log("Saved " + i + " building(s)");
            }
        }

        public override BuildingEntity CreateBuildingEntity(BuildingSaveData saveData, bool initialize)
        {
            if (!initialize)
                new CreateBuildingJob(Database, Assets.onlineScene.SceneName, saveData).Start();
            return base.CreateBuildingEntity(saveData, initialize);
        }

        public override void DestroyBuildingEntity(string id)
        {
            base.DestroyBuildingEntity(id);
            new DeleteBuildingJob(Database, Assets.onlineScene.SceneName, id).Start();
        }
    }
}
