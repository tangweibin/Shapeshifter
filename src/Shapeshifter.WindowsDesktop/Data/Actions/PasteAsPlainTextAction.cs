﻿namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Clipboard.Interfaces;
	using Shapeshifter.WindowsDesktop.Native.Interfaces;

	class PasteAsPlainTextAction: IPasteAsPlainTextAction
    {
        readonly IClipboardInjectionService clipboardInjectionService;
        readonly IClipboardPasteService clipboardPasteService;
        readonly IAsyncFilter asyncFilter;
		readonly IClipboardNativeApi clipboardNativeApi;

		public PasteAsPlainTextAction(
            IClipboardInjectionService clipboardInjectionService,
            IClipboardPasteService clipboardPasteService,
            IAsyncFilter asyncFilter,
			IClipboardNativeApi clipboardNativeApi)
        {
            this.clipboardInjectionService = clipboardInjectionService;
            this.clipboardPasteService = clipboardPasteService;
            this.asyncFilter = asyncFilter;
			this.clipboardNativeApi = clipboardNativeApi;
		}

        public async Task<string> GetDescriptionAsync(IClipboardDataPackage package)
        {
            return "Pastes clipboard contents as plain text with no formatting.";
        }

        public byte Order => 15;

        public string Title => "Paste as plain text";

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage data)
        {
            return await GetFirstSupportedItem(data) != null;
        }

        async Task<IClipboardData> GetFirstSupportedItem(IClipboardDataPackage data)
        {
            var supportedData = await asyncFilter.FilterAsync(data.Contents, CanPerformAsync);
            return supportedData.FirstOrDefault();
        }

        async Task<bool> CanPerformAsync(
            IClipboardData data)
        {
			var formatName = clipboardNativeApi.GetClipboardFormatName(data.RawFormat);
            return data is IClipboardTextData && formatName == "Rich Text Format";
        }

        public async Task PerformAsync(
            IClipboardDataPackage package)
        {
            var textData = (IClipboardTextData) await GetFirstSupportedItem(package);
            await clipboardInjectionService.InjectTextAsync(textData.Text);

            await clipboardPasteService.PasteClipboardContentsAsync();
        }
    }
}