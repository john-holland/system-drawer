using System;
using System.Collections.Generic;

namespace Continuuuum.Credits
{
    [Serializable]
    public sealed class CreditsListDto
    {
        public string id;
        public string tenantId;
        public string title;
        public string episodeId;
        public List<CreditsSectionDto> sections = new List<CreditsSectionDto>();
        public List<CreditsEntryDto> entries = new List<CreditsEntryDto>();
    }

    [Serializable]
    public sealed class CreditsSectionDto
    {
        public string id;
        public string listId;
        public string title;
        public int sortOrder;
        public float scrollSpeed = 40f;
        public bool isSpecialUi;
        public string quadrantPath = "R.0";
    }

    [Serializable]
    public sealed class CreditsEntryDto
    {
        public string id;
        public string listId;
        public string sectionId;
        public string fullName;
        public string nickName;
        public bool showNickname;
        public bool showFullName = true;
        public bool visible = true;
        public int sortOrder;
        public string quote;
        public List<string> images = new List<string>();
        public string company;
        public string rightsMarks;
        public string years;
        public float? scrollSpeed;
        public string sourceUserId;
        public string sourceKind;

        public bool IsVisible => showFullName || showNickname;

        public string DisplayName
        {
            get
            {
                if (showFullName && showNickname && !string.IsNullOrEmpty(nickName))
                    return $"{fullName} \"{nickName}\"";
                if (showNickname && !string.IsNullOrEmpty(nickName))
                    return nickName;
                return fullName ?? "";
            }
        }
    }

    [Serializable]
    public sealed class CreditsListsResponse
    {
        public List<CreditsListDto> lists = new List<CreditsListDto>();
    }
}
