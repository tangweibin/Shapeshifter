﻿namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Api;

    using Controls.Clipboard.Factories.Interfaces;
    using Controls.Clipboard.Interfaces;

    using Data;
    using Data.Interfaces;

    using Handles.Factories.Interfaces;
    using Handles.Interfaces;

    using Interfaces;

    using Services.Files.Interfaces;
    using Services.Interfaces;

    class FileClipboardDataControlFactory: IFileClipboardDataControlFactory
    {
        readonly IDataSourceService dataSourceService;

        readonly IFileIconService fileIconService;

        readonly IMemoryHandleFactory memoryHandleFactory;

        readonly IClipboardControlFactory<IClipboardFileData, IClipboardFileDataControl>
            clipboardFileControlFactory;

        readonly
            IClipboardControlFactory
                <IClipboardFileCollectionData, IClipboardFileCollectionDataControl>
            clipboardFileCollectionControlFactory;

        public FileClipboardDataControlFactory(
            IDataSourceService dataSourceService,
            IFileIconService fileIconService,
            IMemoryHandleFactory memoryHandleFactory,
            IClipboardControlFactory<IClipboardFileData, IClipboardFileDataControl>
                clipboardFileControlFactory,
            IClipboardControlFactory
                <IClipboardFileCollectionData, IClipboardFileCollectionDataControl>
                clipboardFileCollectionControlFactory)
        {
            this.dataSourceService = dataSourceService;
            this.fileIconService = fileIconService;
            this.memoryHandleFactory = memoryHandleFactory;
            this.clipboardFileControlFactory = clipboardFileControlFactory;
            this.clipboardFileCollectionControlFactory = clipboardFileCollectionControlFactory;
        }

        public IClipboardControl BuildControl(IClipboardData clipboardData)
        {
            var clipboardFileCollectionData = clipboardData as IClipboardFileCollectionData;
            if (clipboardFileCollectionData != null)
            {
                return clipboardFileCollectionControlFactory.CreateControl(
                    clipboardFileCollectionData);
            }

            var clipboardFileData = clipboardData as IClipboardFileData;
            if (clipboardFileData != null)
            {
                return clipboardFileControlFactory.CreateControl(
                    clipboardFileData);
            }

            throw new ArgumentException(
                "Unknown clipboard data type.",
                nameof(clipboardData));
        }

        [ExcludeFromCodeCoverage]
        public IClipboardData BuildData(
            uint format,
            byte[] rawData)
        {
            if (!CanBuildData(format))
            {
                throw new ArgumentException(
                    "Can't construct data from this format.",
                    nameof(format));
            }

            var files = GetFilesCopiedFromRawData(rawData);
            if (!files.Any())
            {
                return null;
            }

            return ConstructDataFromFiles(files, format, rawData);
        }

        [ExcludeFromCodeCoverage]
        IReadOnlyCollection<string> GetFilesCopiedFromRawData(byte[] data)
        {
            var files = new List<string>();
            using (var memoryHandle = memoryHandleFactory.AllocateInMemory(data))
            {
                var count = ClipboardApi.DragQueryFile(memoryHandle.Pointer, 0xFFFFFFFF, null, 0);
                FetchFilesFromMemory(files, memoryHandle, count);
            }

            return files;
        }

        [ExcludeFromCodeCoverage]
        static void FetchFilesFromMemory(
            ICollection<string> files,
            IMemoryHandle memoryHandle,
            int count)
        {
            for (var i = 0u; i < count; i++)
            {
                var length = ClipboardApi.DragQueryFile(memoryHandle.Pointer, i, null, 0);
                var filenameBuilder = new StringBuilder(length);

                length = ClipboardApi.DragQueryFile(
                    memoryHandle.Pointer,
                    i,
                    filenameBuilder,
                    length + 1);

                var fileName = filenameBuilder.ToString(0, length);
                files.Add(fileName);
            }
        }

        [ExcludeFromCodeCoverage]
        IClipboardData ConstructDataFromFiles(
            IReadOnlyCollection<string> files,
            uint format,
            byte[] rawData)
        {
            if (files.Count == 1)
            {
                return ConstructClipboardFileData(
                    files.Single(),
                    format,
                    rawData);
            }

            return ConstructClipboardFileCollectionData(
                files,
                format,
                rawData);
        }

        [ExcludeFromCodeCoverage]
        IClipboardData ConstructClipboardFileCollectionData(
            IEnumerable<string> files,
            uint format,
            byte[] rawData)
        {
            return new ClipboardFileCollectionData(dataSourceService)
            {
                Files = files.Select(ConstructClipboardFileData)
                             .ToArray(),
                RawFormat = format,
                RawData = rawData
            };
        }

        [ExcludeFromCodeCoverage]
        IClipboardFileData ConstructClipboardFileData(
            string file,
            uint format,
            byte[] rawData)
        {
            return new ClipboardFileData(dataSourceService)
            {
                FileName = Path.GetFileName(file),
                FullPath = file,
                FileIcon = fileIconService.GetIcon(file, false),
                RawFormat = format,
                RawData = rawData
            };
        }

        [ExcludeFromCodeCoverage]
        IClipboardFileData ConstructClipboardFileData(
            string file)
        {
            return ConstructClipboardFileData(file, 0, null);
        }

        public bool CanBuildControl(IClipboardData data)
        {
            return
                data is IClipboardFileData ||
                data is IClipboardFileCollectionData;
        }

        public bool CanBuildData(uint format)
        {
            return
                format == ClipboardApi.CF_HDROP;
        }
    }
}