using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace NovelEditor
{
    internal class DialogueImage : NovelImage
    {
        Sprite NonameDialogueSprite;
        Sprite DialogueSprite;

        void Awake()
        {
            Init();
        }

        internal void SetDialogueSprite(Sprite dialogueSprite, Sprite nonameDialogueSprite)
        {
            this.DialogueSprite = dialogueSprite;
            this.NonameDialogueSprite = nonameDialogueSprite;
        }

        internal void ChangeDialogueSprite(bool hasName)
        {
            if (hasName)
            {
                Change(DialogueSprite);
            }
            else
            {
                Change(NonameDialogueSprite);
            }

        }
    }
}

