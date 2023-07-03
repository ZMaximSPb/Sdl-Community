﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.UI.WebControls;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.AutoML.V1;
using Google.Cloud.Translate.V3;
using GoogleCloudTranslationProvider.Helpers;
using GoogleCloudTranslationProvider.Interfaces;
using GoogleCloudTranslationProvider.Models;
using NLog;
using Sdl.LanguagePlatform.Core;

namespace GoogleCloudTranslationProvider.GoogleAPI
{
	public class V3Connector
	{
		private readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private readonly ITranslationOptions _options;

		private TranslationServiceClient _translationServiceClient;
		private List<V3LanguageModel> _supportedLanguages;

		public V3Connector(ITranslationOptions options)
		{
			_options = options;
			ConnectToApi();
		}

		#region Connection
		private void ConnectToApi()
		{
			try
			{
				Environment.SetEnvironmentVariable(Constants.GoogleApiEnvironmentVariableName, _options.JsonFilePath);
				_translationServiceClient = TranslationServiceClient.Create();
			}
			catch (Exception e)
			{
				_logger.Error($"{MethodBase.GetCurrentMethod().Name}: {e}");
				ErrorHandler.HandleError(e);
			}
		}

		public void TryToAuthenticateUser(LanguagePair[] languagePair = null)
		{
			languagePair ??= new LanguagePair[]
				{
					new LanguagePair("en-US", "fr-FR")
				};

			foreach (var pair in languagePair)
			{
				TranslateText(pair.SourceCulture, pair.TargetCulture, "test", "text");
			}
		}
		#endregion

		#region Languages
		public bool IsSupportedLanguage(CultureInfo sourceLanguage, CultureInfo targetLanguage)
		{
			_supportedLanguages ??= new();
			if (!_supportedLanguages.Any())
			{
				SetGoogleAvailableLanguages();
			}

			var searchedSource = _supportedLanguages.FirstOrDefault(x => x.CultureInfo.Name.Equals(sourceLanguage.TwoLetterISOLanguageName));
			var searchedTarget = _supportedLanguages.FirstOrDefault(x => x.CultureInfo.Name.Equals(targetLanguage.TwoLetterISOLanguageName));

			return searchedSource.SupportSource && searchedTarget.SupportTarget;
		}

		private void SetGoogleAvailableLanguages()
		{
			try
			{
				TrySetGoogleAvailableLanguages();
			}
			catch (Exception e)
			{
				_logger.Error($"{MethodBase.GetCurrentMethod().Name}: {e}");
			}
		}

		private void TrySetGoogleAvailableLanguages()
		{
			_supportedLanguages ??= new();
			var locationName = new LocationName(_options.ProjectId, "global");
			var request = new GetSupportedLanguagesRequest { ParentAsLocationName = locationName };
			var response = _translationServiceClient.GetSupportedLanguages(request);

			_supportedLanguages.AddRange(response.Languages.Select(language => new V3LanguageModel
			{
				GoogleLanguageCode = language.LanguageCode,
				SupportSource = language.SupportSource,
				SupportTarget = language.SupportTarget,
				CultureInfo = new CultureInfo(language.LanguageCode)
			}));
		}
		#endregion

		#region Translation
		public string TranslateText(CultureInfo sourceLanguage, CultureInfo targetLanguage, string sourceText, string format)
		{
			try
			{
				return TryTranslateText(sourceLanguage, targetLanguage, sourceText, format);
			}
			catch (Exception e)
			{
				_logger.Error($"{MethodBase.GetCurrentMethod().Name}: {e}");
				throw e;
			}
		}

		private string TryTranslateText(CultureInfo sourceLanguage, CultureInfo targetLanguage, string sourceText, string format)
		{
			var request = CreateTranslationRequest(sourceLanguage, targetLanguage, sourceText, format);
			if (_translationServiceClient.TranslateText(request) is not TranslateTextResponse translationResponse)
			{
				return string.Empty;
			}

			return request.GlossaryConfig is null ? translationResponse.Translations[0].TranslatedText
												  : translationResponse.GlossaryTranslations[0].TranslatedText;
		}

		private TranslateTextRequest CreateTranslationRequest(CultureInfo sourceLanguage, CultureInfo targetLanguage, string sourceText, string format)
		{
			return new TranslateTextRequest
			{
				Contents = { sourceText },
				SourceLanguageCode = sourceLanguage.ConvertLanguageCode(),
				TargetLanguageCode = targetLanguage.ConvertLanguageCode(),
				ParentAsLocationName = new LocationName(_options.ProjectId, _options.ProjectLocation),
				MimeType = format == "text" ? "text/plain" : "text/html",
				Model = SetCustomModel(sourceLanguage, targetLanguage),
				GlossaryConfig = SetGlossary(sourceLanguage, targetLanguage)
			};
		}
		#endregion

		#region Glossaries
		public List<Glossary> GetProjectGlossaries(string location = null)
		{
			var locationName = LocationName.FromProjectLocation(_options.ProjectId, location ?? _options.ProjectLocation);
			var glossariesRequest = new ListGlossariesRequest { ParentAsLocationName = locationName };

			return _translationServiceClient.ListGlossaries(glossariesRequest).ToList();
		}

		private TranslateTextGlossaryConfig SetGlossary(CultureInfo sourceLanguage, CultureInfo targetLanguage)
		{
			var selectedGlossary = _options.LanguageMappingPairs?
										   .FirstOrDefault(x => x.LanguagePair.SourceCulture == sourceLanguage && x.LanguagePair.TargetCulture == targetLanguage)?
										   .SelectedGlossary
										   .Glossary;
			
			if (selectedGlossary is null
			 || string.IsNullOrEmpty(selectedGlossary.Name))
			{
				return null;
			}

			return new TranslateTextGlossaryConfig
			{
				Glossary = selectedGlossary.Name,
				IgnoreCase = true
			};
		}
		#endregion

		#region AutoML
		public List<Model> GetProjectCustomModels()
		{
			var request = new ListModelsRequest
			{
				ParentAsLocationName = new LocationName(_options.ProjectId, _options.ProjectLocation)
			};

			return AutoMlClient.Create().ListModels(request).ToList();
		}

		private string SetCustomModel(CultureInfo sourceLanguage, CultureInfo targetLanguage)
		{
			var defaultPath = $"projects/{_options.ProjectId}/locations/{_options.ProjectLocation}/models/general/nmt";
			var selectedModel = _options.LanguageMappingPairs?
										   .FirstOrDefault(x => x.LanguagePair.SourceCulture == sourceLanguage && x.LanguagePair.TargetCulture == targetLanguage)?
										   .SelectedModel?
										   .ModelPath;
			return selectedModel switch
			{
				not null => selectedModel,
				_ => defaultPath
			};
		}
		#endregion
	}
}