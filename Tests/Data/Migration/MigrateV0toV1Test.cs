﻿using System;
using System.IO;
using NUnit.Framework;
using SQLite.Net;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe.Data;
using XPlatUtils;
using V0 = Toggl.Phoebe.Data.Models.Old.DB_VERSION_0;
using V1 = Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Tests.Data.Migration
{
    [TestFixture]
    public class MigrateV0toV1Test
    {
        private string databasePath;

        #region Setup

        [SetUp]
        public void SetUp()
        {
            ServiceContainer.RegisterScoped<MessageBus>(new MessageBus());
            ServiceContainer.Register<ITimeProvider>(new DefaultTimeProvider());
            ServiceContainer.Register<TimeCorrectionManager>(new TimeCorrectionManager());

            this.setupV0database();
        }

        private void setupV0database()
        {
            var path = Path.GetTempFileName();

            var db = new SQLiteConnection(new SQLitePlatformGeneric(), path);

            db.CreateTable<V0.ClientData>();
            db.CreateTable<V0.ProjectData>();
            db.CreateTable<V0.ProjectUserData>();
            db.CreateTable<V0.TagData>();
            db.CreateTable<V0.TaskData>();
            db.CreateTable<V0.TimeEntryData>();
            db.CreateTable<V0.TimeEntryTagData>();
            db.CreateTable<V0.UserData>();
            db.CreateTable<V0.WorkspaceData>();
            db.CreateTable<V0.WorkspaceUserData>();

            db.Close();

            this.databasePath = path;
        }

        #endregion

        #region Tests

        [Test]
        public void TestMigrateEmpty()
        {
            var store = this.migrate();
            Assert.That(1, Is.EqualTo(store.GetVersion()));
        }

        [Test]
        public void TestMigrateWorkspaceData()
        {
            var workspaceData = new V0.WorkspaceData
            {
                Id = Guid.NewGuid(),
                Name = "the matrix",
                BillableRatesVisibility = V1.AccessLevel.Admin,
                DefaultCurrency = "currency",
                DefaultRate = null,
                IsPremium = true,
                LogoUrl = "http://toggl.com",
                ProjectCreationPrivileges = V1.AccessLevel.Regular,
                RoundingMode = RoundingMode.Down,
                RoundingPercision = 1
            };

            this.insertIntoDatabase(workspaceData);
            var store = this.migrate();
            var newWorkspaceData = store.Table<V1.WorkspaceData>().First();

            Assert.That(workspaceData.Id, Is.EqualTo(newWorkspaceData.Id));
            Assert.That(workspaceData.Name, Is.EqualTo(newWorkspaceData.Name));
            Assert.That(workspaceData.BillableRatesVisibility, Is.EqualTo(newWorkspaceData.BillableRatesVisibility));
            Assert.That(workspaceData.DefaultCurrency, Is.EqualTo(newWorkspaceData.DefaultCurrency));
            Assert.That(workspaceData.DefaultRate, Is.EqualTo(newWorkspaceData.DefaultRate));
            Assert.That(workspaceData.IsPremium, Is.EqualTo(newWorkspaceData.IsPremium));
            Assert.That(workspaceData.LogoUrl, Is.EqualTo(newWorkspaceData.LogoUrl));
            Assert.That(workspaceData.ProjectCreationPrivileges, Is.EqualTo(newWorkspaceData.ProjectCreationPrivileges));
            Assert.That(workspaceData.RoundingMode, Is.EqualTo(newWorkspaceData.RoundingMode));
            Assert.That(workspaceData.RoundingPercision, Is.EqualTo(newWorkspaceData.RoundingPrecision));
        }

        [Test]
        public void TestMigrateUserData()
        {
            var workspaceData = new V0.WorkspaceData { Id = Guid.NewGuid(), RemoteId = 12L };
            var userData = new V0.UserData
            {
                Name = "user",
                DefaultWorkspaceId = workspaceData.Id,
                DateFormat = "dateFormat",
                DurationFormat = DurationFormat.Improved,
                Email = "user@toggl.com",
                ExperimentIncluded = false,
                ExperimentNumber = 1,
                SendProductEmails = false,
                SendTimerNotifications = false,
                SendWeeklyReport = false,
                StartOfWeek = DayOfWeek.Monday,
                TimeFormat = "timeFormat",
                Timezone = "timeZone",
                TrackingMode = V1.TrackingMode.Continue, // is that ok?
                Locale = "locale",
                ImageUrl = "ImageUrl"
            };

            insertIntoDatabase(
                workspaceData,
                userData
            );

            var store = migrate();
            var newUserData = store.Table<V1.UserData>().First();

            // test relationships
            Assert.That(userData.DefaultWorkspaceId, Is.EqualTo(newUserData.DefaultWorkspaceId));
            Assert.That(workspaceData.RemoteId, Is.EqualTo(newUserData.DefaultWorkspaceRemoteId));

            Assert.That(userData.Name, Is.EqualTo(newUserData.Name));
            Assert.That(userData.DateFormat, Is.EqualTo(newUserData.DateFormat));
            Assert.That(userData.DurationFormat, Is.EqualTo(newUserData.DurationFormat));
            Assert.That(userData.Email, Is.EqualTo(newUserData.Email));
            Assert.That(userData.ExperimentIncluded, Is.EqualTo(newUserData.ExperimentIncluded));
            Assert.That(userData.ExperimentNumber, Is.EqualTo(newUserData.ExperimentNumber));
            Assert.That(userData.ImageUrl, Is.EqualTo(newUserData.ImageUrl));
            Assert.That(userData.Locale, Is.EqualTo(newUserData.Locale));
            Assert.That(userData.SendProductEmails, Is.EqualTo(newUserData.SendProductEmails));
            Assert.That(userData.SendTimerNotifications, Is.EqualTo(newUserData.SendTimerNotifications));
            Assert.That(userData.SendWeeklyReport, Is.EqualTo(newUserData.SendWeeklyReport));
            Assert.That(userData.StartOfWeek, Is.EqualTo(newUserData.StartOfWeek));
            Assert.That(userData.TimeFormat, Is.EqualTo(newUserData.TimeFormat));
            Assert.That(userData.Timezone, Is.EqualTo(newUserData.Timezone));
            Assert.That(userData.TrackingMode, Is.EqualTo(newUserData.TrackingMode));
        }

        [Test]
        public void TestMigrateWorkspaceUserData()
        {
            var workspaceID = Guid.NewGuid();
            var workspaceRemoteID = 42L;
            var userRemoteID = 1337L;
            var userID = Guid.NewGuid();

            this.insertIntoDatabase(
                new V0.WorkspaceData { Id = workspaceID, RemoteId = workspaceRemoteID },
                new V0.UserData { Id = userID, RemoteId = userRemoteID },
                new V0.WorkspaceUserData { UserId = userID, WorkspaceId = workspaceID }
            );

            var store = this.migrate();

            var wsUserData = store.Table<V1.WorkspaceUserData>().First();

            Assert.That(userID, Is.EqualTo(wsUserData.UserId));
            Assert.That(userRemoteID, Is.EqualTo(wsUserData.UserRemoteId));
            Assert.That(workspaceID, Is.EqualTo(wsUserData.WorkspaceId));
            Assert.That(workspaceRemoteID, Is.EqualTo(wsUserData.WorkspaceRemoteId));
        }

        [Test]
        public void TestMigrateClientData()
        {
            var workspaceID = Guid.NewGuid();
            var workspaceRemoteID = 42L;
            var clientRemoteId = 1337L;
            var clientName = "the oracle";

            this.insertIntoDatabase(
                new V0.WorkspaceData { Id = workspaceID, RemoteId = workspaceRemoteID },
                new V0.ClientData { RemoteId = clientRemoteId, Name = clientName, WorkspaceId = workspaceID }
            );

            var store = this.migrate();
            var client = store.Table<V1.ClientData>().First();

            Assert.That(clientRemoteId, Is.EqualTo(client.RemoteId));
            Assert.That(clientName, Is.EqualTo(client.Name));
            Assert.That(workspaceID, Is.EqualTo(client.WorkspaceId));
            Assert.That(workspaceRemoteID, Is.EqualTo(client.WorkspaceRemoteId));
        }

        [Test]
        public void TestMigrateProjectData()
        {
            var workspaceID = Guid.NewGuid();
            var workspaceRemoteID = 42L;
            var clientId = Guid.NewGuid();
            var clientRemoteId = 1337L;
            var projectRemoteId = 500L;
            var projectName = "save the world";

            var projectData = new V0.ProjectData
            {
                RemoteId = projectRemoteId,
                Name = projectName,
                ClientId = clientId,
                WorkspaceId = workspaceID,
                Color = 0,
                IsActive = true,
                IsBillable = true,
                IsPrivate = true,
                IsTemplate = true,
                UseTasksEstimate = true
            };

            this.insertIntoDatabase(
                new V0.WorkspaceData { Id = workspaceID, RemoteId = workspaceRemoteID },
                new V0.ClientData { Id = clientId, RemoteId = clientRemoteId, WorkspaceId = workspaceID },
                projectData
            );

            var store = this.migrate();
            var newProjectData = store.Table<V1.ProjectData>().First();

            // Check relationships
            Assert.That(clientId, Is.EqualTo(newProjectData.ClientId));
            Assert.That(clientRemoteId, Is.EqualTo(newProjectData.ClientRemoteId));
            Assert.That(workspaceID, Is.EqualTo(newProjectData.WorkspaceId));
            Assert.That(workspaceRemoteID, Is.EqualTo(newProjectData.WorkspaceRemoteId));
            Assert.That(projectData.ClientId, Is.EqualTo(newProjectData.ClientId));
            Assert.That(projectData.WorkspaceId, Is.EqualTo(newProjectData.WorkspaceId));

            Assert.That(projectData.IsActive, Is.EqualTo(newProjectData.IsActive));
            Assert.That(projectData.IsBillable, Is.EqualTo(newProjectData.IsBillable));
            Assert.That(projectData.IsPrivate, Is.EqualTo(newProjectData.IsPrivate));
            Assert.That(projectData.IsTemplate, Is.EqualTo(newProjectData.IsTemplate));
            Assert.That(projectData.Color, Is.EqualTo(newProjectData.Color));
            Assert.That(projectData.UseTasksEstimate, Is.EqualTo(newProjectData.UseTasksEstimate));
        }

        [Test]
        public void TestMigrateProjectUserData()
        {
            var projectId = Guid.NewGuid();
            var projectRemoteId = 500L;
            var projectName = "save the world";
            var userRemoteID = 1337L;
            var userID = Guid.NewGuid();

            this.insertIntoDatabase(
                new V0.ProjectData { Id = projectId, RemoteId = projectRemoteId, Name = projectName },
                new V0.UserData { Id = userID, RemoteId = userRemoteID },
                new V0.ProjectUserData { UserId = userID, ProjectId = projectId }
            );

            var store = this.migrate();

            var projectUserData = store.Table<V1.ProjectUserData>().First();

            Assert.That(projectId, Is.EqualTo(projectUserData.ProjectId));
            Assert.That(projectRemoteId, Is.EqualTo(projectUserData.ProjectRemoteId));
            Assert.That(userID, Is.EqualTo(projectUserData.UserId));
            Assert.That(userRemoteID, Is.EqualTo(projectUserData.UserRemoteId));
        }

        [Test]
        public void TestMigrateTagData()
        {
            var workspaceID = Guid.NewGuid();
            var workspaceRemoteID = 42L;
            var tagRemoteId = 500L;
            var tagName = "epic";
            var tagData = new V0.TagData { Id = Guid.NewGuid(), RemoteId = tagRemoteId, Name = tagName, WorkspaceId = workspaceID };

            this.insertIntoDatabase(
                new V0.WorkspaceData { Id = workspaceID, RemoteId = workspaceRemoteID },
                tagData
            );

            var store = this.migrate();
            var newTagData = store.Table<V1.TagData>().First();

            // test relationships
            Assert.That(workspaceID, Is.EqualTo(newTagData.WorkspaceId));
            Assert.That(workspaceRemoteID, Is.EqualTo(newTagData.WorkspaceRemoteId));

            Assert.That(tagName, Is.EqualTo(newTagData.Name));
        }

        [Test]
        public void TestMigrateTaskData()
        {
            var workspaceID = Guid.NewGuid();
            var workspaceRemoteID = 42L;
            var projectID = Guid.NewGuid();
            var projectRemoteId = 500L;
            var taskRemoteId = 1337L;
            var taskName = "become the one";

            this.insertIntoDatabase(
                new V0.WorkspaceData { Id = workspaceID, RemoteId = workspaceRemoteID },
                new V0.ProjectData { Id = projectID, RemoteId = projectRemoteId, WorkspaceId = workspaceID },
                new V0.TaskData { RemoteId = taskRemoteId, Name = taskName, WorkspaceId = workspaceID, ProjectId = projectID }
            );

            var store = this.migrate();
            var task = store.Table<V1.TaskData>().First();

            Assert.That(taskRemoteId, Is.EqualTo(task.RemoteId));
            Assert.That(taskName, Is.EqualTo(task.Name));
            Assert.That(projectID, Is.EqualTo(task.ProjectId));
            Assert.That(projectRemoteId, Is.EqualTo(task.ProjectRemoteId));
            Assert.That(workspaceID, Is.EqualTo(task.WorkspaceId));
            Assert.That(workspaceRemoteID, Is.EqualTo(task.WorkspaceRemoteId));
        }

        [Test]
        public void TestMigrateTimeEntryData()
        {
            var workspaceID = Guid.NewGuid();
            var workspaceRemoteID = 42L;
            var projectID = Guid.NewGuid();
            var projectRemoteId = 500L;
            var taskId = Guid.NewGuid();
            var taskRemoteId = 1337L;
            var tagId = Guid.NewGuid();
            var tagName = "epic";

            var timeEntryId = Guid.NewGuid();
            var timeEntryDescription = "learning kung fu";
            var timeEntryData = new V0.TimeEntryData
            {
                Id = timeEntryId,
                Description = timeEntryDescription,
                WorkspaceId = workspaceID,
                ProjectId = projectID,
                TaskId = taskId,
                State = V1.TimeEntryState.Finished, // Using old version state?
                IsBillable = true,
                StartTime = DateTime.Now,
                StopTime = null,
                DurationOnly = false,
                IsDirty = true
            };

            this.insertIntoDatabase(
                new V0.WorkspaceData { Id = workspaceID, RemoteId = workspaceRemoteID },
                new V0.ProjectData { Id = projectID, RemoteId = projectRemoteId, WorkspaceId = workspaceID },
                new V0.TaskData { Id = taskId, RemoteId = taskRemoteId, WorkspaceId = workspaceID, ProjectId = projectID },
                new V0.TagData { Id = tagId, Name = tagName, WorkspaceId = workspaceID },
                timeEntryData,
                new V0.TimeEntryTagData { TimeEntryId = timeEntryId, TagId = tagId }
            );

            var store = this.migrate();
            var newTimeEntryData = store.Table<V1.TimeEntryData>().First();

            // test relationships
            Assert.That(1, Is.EqualTo(newTimeEntryData.Tags.Count));
            Assert.That(tagName, Is.EqualTo(newTimeEntryData.Tags[0]));
            Assert.That(timeEntryData.TaskId, Is.EqualTo(newTimeEntryData.TaskId));
            Assert.That(taskRemoteId, Is.EqualTo(newTimeEntryData.TaskRemoteId));
            Assert.That(timeEntryData.ProjectId, Is.EqualTo(newTimeEntryData.ProjectId));
            Assert.That(projectRemoteId, Is.EqualTo(newTimeEntryData.ProjectRemoteId));
            Assert.That(timeEntryData.WorkspaceId, Is.EqualTo(newTimeEntryData.WorkspaceId));
            Assert.That(workspaceRemoteID, Is.EqualTo(newTimeEntryData.WorkspaceRemoteId));

            Assert.That(timeEntryData.Id, Is.EqualTo(newTimeEntryData.Id));
            Assert.That(timeEntryData.Description, Is.EqualTo(newTimeEntryData.Description));
            Assert.That(timeEntryData.DurationOnly, Is.EqualTo(newTimeEntryData.DurationOnly));
            Assert.That(timeEntryData.IsBillable, Is.EqualTo(newTimeEntryData.IsBillable));
            Assert.That(timeEntryData.State, Is.EqualTo(newTimeEntryData.State));
        }

        [Test]
        public void TestCommonData()
        {
            var tagData = new V0.TagData
            {
                Id = Guid.NewGuid(),
                RemoteId = 12,
                IsDirty = false,
                DeletedAt = null,
                ModifiedAt = DateTime.UtcNow,
                RemoteRejected = true
            };

            insertIntoDatabase(tagData);
            var store = this.migrate();
            var newTagData = store.Table<V1.TagData>().Where(t => t.Id == tagData.Id).First();

            Assert.That(tagData.Id, Is.EqualTo(newTagData.Id));
            Assert.That(tagData.RemoteId, Is.EqualTo(newTagData.RemoteId));
            Assert.That(tagData.DeletedAt, Is.EqualTo(newTagData.DeletedAt));
            Assert.That(tagData.ModifiedAt, Is.EqualTo(newTagData.ModifiedAt));
            Assert.That(newTagData.SyncState, Is.EqualTo(V1.SyncState.Synced));
        }

        [Test]
        public void TestDirtyUpdateData()
        {
            var tagData = new V0.TagData
            {
                Id = Guid.NewGuid(),
                RemoteId = 12,
                IsDirty = true,
                RemoteRejected = true
            };

            insertIntoDatabase(tagData);
            var store = migrate();
            var newTagData = store.Table<V1.TagData>().Where(t => t.Id == tagData.Id).First();
            Assert.That(newTagData.SyncState, Is.EqualTo(V1.SyncState.UpdatePending));
        }

        [Test]
        public void TestDirtyCreateData()
        {
            var tagData = new V0.TagData
            {
                Id = Guid.NewGuid(),
                RemoteId = null,
                IsDirty = true,
                RemoteRejected = true
            };

            insertIntoDatabase(tagData);
            var store = migrate();
            var newTagData = store.Table<V1.TagData>().Where(t => t.Id == tagData.Id).First();
            Assert.That(newTagData.SyncState, Is.EqualTo(V1.SyncState.CreatePending));
        }

        #endregion

        #region Helpers

        private SyncSqliteDataStore migrate()
        {
            return new SyncSqliteDataStore(this.databasePath, new SQLitePlatformGeneric());
        }

        private void insertIntoDatabase(params object[] objects)
        {
            var db = new SQLiteConnection(new SQLitePlatformGeneric(), this.databasePath);

            db.InsertAll(objects);

            db.Close();
        }

        #endregion

    }
}

