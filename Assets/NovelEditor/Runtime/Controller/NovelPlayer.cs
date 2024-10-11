using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;

namespace NovelEditor
{
    /// <summary>
    /// 会話パートを再生するためのコンポーネント
    /// </summary>
    [RequireComponent(typeof(NovelUIManager))]
    public class NovelPlayer : MonoBehaviour
    {
        # region variable
        [SerializeField, HideInInspector] private NovelData _novelData;
        [SerializeField, HideInInspector] private ChoiceButton _choiceButton;
        [SerializeField, HideInInspector] private Sprite _dialogueSprite;
        [SerializeField, HideInInspector] private Sprite _nonameDialogueSprite;
        [SerializeField, HideInInspector] private bool _playOnAwake = true;
        [SerializeField, HideInInspector] private bool _hideAfterPlay = false;
        [SerializeField, HideInInspector] private float _hideFadeTime = 0.5f;
        [SerializeField, HideInInspector] private bool _isDisplay = true;

        [SerializeField, HideInInspector] private float _charaFadeTime = 0.2f;
        [SerializeField, HideInInspector] private int _textSpeed = 6;

        [SerializeField, HideInInspector] private float _BGMVolume = 1;
        [SerializeField, HideInInspector] private float _SEVolume = 1;

        [SerializeField, HideInInspector] private HowInput _inputSystem;
        [SerializeField] private KeyCode[] _nextButton;
        [SerializeField] private KeyCode[] _skipButton;
        [SerializeField] private KeyCode[] _hideOrDisplayButton;
        [SerializeField] private KeyCode[] _stopOrStartButton;

        private NovelInputProvider _inputProvider;
        private NovelUIManager _novelUI;
        private AudioPlayer _audioPlayer;
        private ChoiceManager _choiceManager;

        private NovelData.ParagraphData _nowParagraph;
        private int _nextDialogueNum = 0;

        private bool _isReading = false;
        private bool _isImageChangeing = false;
        private bool _isStop = false;
        private bool _isPlaying = false;
        private bool _isChoicing = false;
        private bool _isUIDisplay = true;
        private bool _mute = false;
        private bool _isEnd = false;
        private bool _isLoading = false;

        private List<string> _choiceName = new();
        private List<string> _ParagraphName = new();
        private List<int> _passedParagraphID = new();

        private CancellationTokenSource _textCTS = new CancellationTokenSource();
        private CancellationTokenSource _imageCTS = new CancellationTokenSource();
        private CancellationTokenSource _choiceCTS = new CancellationTokenSource();
        private CancellationTokenSource _endFadeCTS = new CancellationTokenSource();

        #endregion

        #region property
        /// <summary>
        /// 現在セットしてあるNovelData
        /// </summary>
        public NovelData novelData => _novelData;
        /// <summary>
        /// 現在停止しているか
        /// </summary>
        public bool IsStop => _isStop;
        /// <summary>
        /// 現在再生中か
        /// </summary>
        public bool IsPlaying => _isPlaying;
        /// <summary>
        /// 選択肢が表示されているか
        /// </summary>
        public bool IsChoicing => _isChoicing;
        /// <summary>
        /// ロード中、現在の進捗0〜1で返します。
        /// </summary>
        public float loadProgress => SaveUtility.Instance.progress;
        /// <summary>
        /// UIの表示状態を変更できます
        /// </summary>
        /// <value>UIを表示しているか</value>
        public bool IsUIDisplay
        {
            get
            {
                return _isUIDisplay;
            }

            set
            {
                if (value)
                {
                    UnPause();
                    DisplayUI();
                }
                else
                {
                    Pause();
                    HideUI();
                }

                _isDisplay = value;
            }
        }
        /// <summary>
        /// 背景や立ち絵を含める会話パートのUIの表示状態を変えられます
        /// </summary>
        /// <value>背景や立ち絵を含める会話パートのUIの表示状態</value>
        public bool IsDisplay
        {
            get
            {
                return _isDisplay;
            }

            set
            {
                if (value)
                {
                    UnPause();
                }
                else
                {
                    Pause();
                }
                _novelUI.SetDisplay(value);
                _isDisplay = value;
            }
        }

        /// <summary>
        /// ミュートを切り替えられます
        /// </summary>
        /// <value>ミュート中かどうか</value>
        public bool mute
        {
            get
            {
                return _mute;
            }
            set
            {
                _audioPlayer.SetMute(value);
                _mute = value;
            }
        }

        /// <summary>
        /// BGMの大きさを変えられます
        /// </summary>
        /// <value>BGMの大きさ</value>
        public float BGMVolume
        {
            get
            {
                return _BGMVolume;
            }

            set
            {
                _BGMVolume = Mathf.Clamp(value, 0, 1);
                _audioPlayer.SetBGMVolume(_BGMVolume);
            }
        }

        /// <summary>
        /// SEの大きさを変えられます
        /// </summary>
        /// <value>SEの大きさ</value>
        public float SEVolume
        {
            get
            {
                return _SEVolume;
            }

            set
            {
                _SEVolume = Mathf.Clamp(value, 0, 1);
                _audioPlayer.SetSEVolume(_SEVolume);
            }
        }

        /// <summary>
        /// テキストの再生速度を変えられます
        /// </summary>
        /// <value>テキスト再生速度</value>
        public int textSpeed
        {
            get
            {
                return _textSpeed;
            }
            set
            {
                _textSpeed = value;

                if (value < 1)
                {
                    _textSpeed = 1;
                }

                _novelUI.SetTextSpeed(_textSpeed);
            }
        }

        /// <summary>
        /// 今まで再生した会話ノードのName
        /// </summary>
        public List<string> ParagraphName => _ParagraphName;
        /// <summary>
        /// 今まで選択した選択肢のName
        /// </summary>
        public List<string> ChoiceName => _choiceName;

        #endregion

        #region delegate
        public delegate void OnBeginDelegate();
        /// <summary>
        /// 再生を開始した時に呼び出されます
        /// </summary>
        public OnBeginDelegate OnBegin;

        public delegate void OnLoadDelegate();
        /// <summary>
        /// ロード時に呼び出されます
        /// </summary>
        public OnLoadDelegate OnLoad;

        public delegate void OnEndDelegate();
        /// <summary>
        /// 再生終了時に呼び出されます
        /// </summary>
        public OnEndDelegate OnEnd;

        public delegate void OnDialogueChangedDelegate(NovelData.ParagraphData.Dialogue data);
        /// <summary>
        /// 次のセリフへ進んだ時に呼び出されます
        /// </summary>
        public OnDialogueChangedDelegate OnDialogueChanged;

        public delegate void NodeChangedDelegate(string nodeName);
        /// <summary>
        /// 次の会話ノードへ進んだときに呼び出されます
        /// </summary>
        public NodeChangedDelegate ParagraphNodeChanged;
        /// <summary>
        /// 選択肢を選んだ時に呼び出されます
        /// </summary>
        public NodeChangedDelegate OnChoiced;

        public delegate void SkipedDeleteDelegate();
        /// <summary>
        /// スキップ時に呼ばれます
        /// </summary>
        public SkipedDeleteDelegate OnSkiped;

        #endregion


        #region publicMethod

        /// <summary>
        /// 会話を最初から再生します
        /// </summary>
        /// <param name="data">再生するデータ</param>
        /// <param name="hideAfterPlay">再生終了後に画面を非表示にするか</param>
        public void Play(NovelData data, bool hideAfterPlay)
        {
            _novelData = data;
            _hideAfterPlay = hideAfterPlay;

            Reset();

            UnPause();
            if (OnBegin != null)
                OnBegin();

            SetNextParagraph(0);
            _isPlaying = true;
        }

        /// <summary>
        /// 会話を保存されたデータからロードして再生します
        /// </summary>
        /// <param name="saveData">再生するセーブデータ</param>
        /// <param name="hideAfterPlay">再生終了時に非表示にするか</param>
        public void Load(NovelSaveData saveData, bool hideAfterPlay)
        {
            _isLoading = true;
            _choiceCTS.Cancel();
            _novelData = saveData.novelData;
            _hideAfterPlay = hideAfterPlay;
            _textCTS.Cancel();
            _novelUI.FlashText();

            Reset(true);

            _nextDialogueNum = saveData.dialogueIndex;
            _nowParagraph = _novelData.paragraphList[saveData.paragraphIndex];
            _ParagraphName = saveData.ParagraphName;
            _choiceName = saveData.choiceName;

            //復元、新しいデータをとりあえず再生
            NovelData.ParagraphData.Dialogue newData = SaveUtility.Instance.LoadDialogue(saveData);

            UnPause();

            if (OnLoad != null)
                OnLoad();

            SetNextDialogue(newData);
            _isLoading = false;
            _isPlaying = true;
        }

        /// <summary>
        /// 会話を一時停止する
        /// </summary>
        public void Pause()
        {
            _isStop = true;
            _novelUI.SetStopText(true);
        }

        /// <summary>
        /// 会話の一時停止を解除する
        /// </summary>
        public void UnPause()
        {
            _isStop = false;
            _novelUI.SetStopText(false);
        }

        /// <summary>
        /// UIを非表示にする
        /// </summary>
        public void HideUI()
        {
            _novelUI.SetUIDisplay(false);
            Pause();
            _isUIDisplay = false;
        }

        /// <summary>
        /// UIを表示する
        /// </summary>
        public void DisplayUI()
        {
            _novelUI.SetUIDisplay(true);
            UnPause();
            _isUIDisplay = true;
        }

        /// <summary>
        /// 次の選択肢までスキップします。選択肢がなければ会話を終了します。
        /// </summary>
        public void Skip()
        {
            if (_isChoicing || _isImageChangeing)
            {
                return;
            }

            _isLoading = true;
            _textCTS.Cancel();
            _choiceCTS.Cancel();

            SkipedData newData = SaveUtility.Instance.Skip(_novelData, _nowParagraph.index, _nextDialogueNum, _passedParagraphID, _ParagraphName, _novelUI.GetNowBack());
            if (OnSkiped != null)
                OnSkiped();

            UnPause();

            if (newData.next == Next.Choice)
            {
                _nowParagraph = novelData.paragraphList[newData.ParagraphIndex];
                _nextDialogueNum = _nowParagraph.dialogueList.Count;
                SetNextDialogue(newData.dialogue);
            }
            else
            {
                end();
            }

            _isLoading = false;
        }

        /// <summary>
        /// 次の会話ノードまでスキップします。次のノードが選択肢なら選択肢までスキップします。
        /// </summary>
        public void SkipNextNode()
        {
            if (_isChoicing || _isImageChangeing)
            {
                return;
            }
            _isLoading = true;
            _textCTS.Cancel();
            _choiceCTS.Cancel();

            if (_nowParagraph.next == Next.End || _nowParagraph.nextParagraphIndex == -1)
            {
                end();
                return;
            }

            NovelData.ParagraphData.Dialogue newData = SaveUtility.Instance.SkipNextNode(novelData, _nowParagraph, _nextDialogueNum, _novelUI.GetNowBack());

            if (OnSkiped != null)
                OnSkiped();

            UnPause();

            switch (_nowParagraph.next)
            {
                case Next.Choice:
                    _nextDialogueNum = _nowParagraph.dialogueList.Count;
                    break;
                case Next.Continue:
                    _nowParagraph = _novelData.paragraphList[_nowParagraph.nextParagraphIndex];
                    _nextDialogueNum = 0;
                    _passedParagraphID.Add(_nowParagraph.index);
                    _ParagraphName.Add(_nowParagraph.nodeName);
                    if (ParagraphNodeChanged != null)
                        ParagraphNodeChanged(_nowParagraph.nodeName);
                    break;
            }

            SetNextDialogue(newData);
            _isLoading = false;

        }

        /// <summary>
        /// 現在の状態をセーブします。
        /// </summary>
        /// <returns>セーブデータ</returns>
        public NovelSaveData save()
        {
            int nowDialogueNum = _nextDialogueNum - 1;
            if (nowDialogueNum == _nowParagraph.dialogueList.Count)
            {
                nowDialogueNum--;
            }
            return SaveUtility.Instance.SaveDialogue(novelData, _nowParagraph.index, nowDialogueNum, _passedParagraphID, ChoiceName, ParagraphName);
        }


        /// <summary>
        /// 入力方法を設定できます
        /// </summary>
        /// <param name="input">NovelInputProviderを継承して入力方法を定義したクラスのインスタンス</param>
        public void SetInputProvider(NovelInputProvider input)
        {
            _inputProvider = input;
        }

        #endregion


        #region privateMethod
        void Awake()
        {
            //入力方法の指定
            switch (_inputSystem)
            {
                case HowInput.UserSetting:
                    _inputProvider = new CustomInputProvider(_nextButton, _skipButton, _hideOrDisplayButton, _stopOrStartButton);
                    break;
                case HowInput.Default:
                    _inputProvider = new DefaultInputProvider();
                    break;
                case HowInput.OverWrite:
                    if (_inputProvider == null)
                    {
                        _inputProvider = new DefaultInputProvider();
                    }
                    break;
            }

            //UIの設定、表示
            _novelUI = GetComponent<NovelUIManager>();
            _novelUI.Init(_charaFadeTime, _nonameDialogueSprite, _dialogueSprite);
            SetDisplay(_isDisplay);

            //初期化処理
            _audioPlayer = gameObject.AddComponent<AudioPlayer>();
            _audioPlayer.Init(_BGMVolume, _SEVolume);

            _choiceManager = GetComponentInChildren<ChoiceManager>();
            _choiceManager.Init(_choiceButton);

            //PlayOnAwakeの場合再生
            if (_playOnAwake && _novelData != null)
            {
                Play(_novelData, _hideAfterPlay);
            }
        }

        /// <summary>
        /// 背景や立ち絵を含む全てのUIの表示を設定する
        /// </summary>
        /// <param name="isDisplay">表示するかどうか</param>
        void SetDisplay(bool isDisplay)
        {
            if (isDisplay)
            {
                UnPause();

                _novelUI.SetDisplay(isDisplay);
                _isDisplay = true;
            }
            else
            {
                _endFadeCTS.Cancel();
                Pause();
                _novelUI.SetDisplay(isDisplay);
                _isDisplay = false;
            }
        }

        /// <summary>
        /// 現在再生しているものをリセット
        /// </summary>
        /// <param name="isLoad">ロード後かどうか</param>
        void Reset(bool isLoad = false)
        {
            _novelUI.Reset(_novelData.locations, isLoad);

            //選択肢を全部消す
            _choiceCTS.Cancel();
            _choiceManager.ResetChoice();
            _isChoicing = false;
            _isEnd = false;

            //今までのやつを消す
            _choiceName.Clear();
            _ParagraphName.Clear();

            SetDisplay(true);
        }

        void Update()
        {
            if (!_isPlaying || _isImageChangeing || !_isDisplay)
            {
                return;
            }

            if (_inputProvider.GetStopOrStart())
            {
                _isStop = !_isStop;
                _novelUI.SwitchStopText();
            }

            if (_inputProvider.GetHideOrDisplay())
            {
                if (_isUIDisplay)
                    HideUI();
                else if (!_isUIDisplay)
                    DisplayUI();
            }

            if (_isChoicing || !_isUIDisplay || _isLoading || _isStop)
            {
                return;
            }

            if (_inputProvider.GetNext())
            {
                //全部表示
                if (_isReading && _novelUI.canFlush)
                {
                    FlashText();
                }
                else if (!_isReading)
                {
                    SetNext();
                }

            }
            if (_inputProvider.GetSkip())
            {
                Skip();
            }
        }

        /// <summary>
        /// 次の会話ノードを再生する
        /// </summary>
        /// <param name="nextIndex">次の会話ノードのIndex</param>
        void SetNextParagraph(int nextIndex)
        {
            if (nextIndex == -1)
            {
                end();
            }
            else
            {
                _nowParagraph = _novelData.paragraphList[nextIndex];
                _ParagraphName.Add(_nowParagraph.nodeName);
                _passedParagraphID.Add(_nowParagraph.index);
                if (ParagraphNodeChanged != null)
                    ParagraphNodeChanged(_nowParagraph.nodeName);
                _nextDialogueNum = 0;
                SetNextDialogue(_nowParagraph.dialogueList[_nextDialogueNum]);
            }
        }

        /// <summary>
        /// 次のセリフ、あるいはノードを再生する
        /// </summary>
        void SetNext()
        {
            if (_nextDialogueNum >= _nowParagraph.dialogueList.Count)
            {
                switch (_nowParagraph.next)
                {
                    case Next.Choice:
                        SetChoice();
                        break;
                    case Next.Continue:
                        SetNextParagraph(_nowParagraph.nextParagraphIndex);
                        break;
                    case Next.End:
                        end();
                        break;
                }
            }
            else
            {
                SetNextDialogue(_nowParagraph.dialogueList[_nextDialogueNum]);
            }
        }

        /// <summary>
        /// 選択肢を設定する
        /// </summary>
        async void SetChoice()
        {
            _isChoicing = true;
            _choiceCTS = new CancellationTokenSource();

            List<NovelData.ChoiceData> list = new();

            //次の選択肢のリストを作成する
            foreach (int i in _nowParagraph.nextChoiceIndexes)
            {
                if (i == -1)
                    continue;
                list.Add(_novelData.choiceList[i]);
            }
            if (list.Count == 0)
            {
                end();
                return;
            }

            //選択を待ち、それに応じて次のノードを再生する
            var ans = await _choiceManager.WaitChoice(list, _choiceCTS.Token);
            if (ans != null)
            {
                _choiceName.Add(ans.nodeName);
                if (OnChoiced != null)
                    OnChoiced(ans.nodeName);
                SetNextParagraph(ans.nextParagraphIndex);
            }

            _isChoicing = false;
        }


        /// <summary>
        /// 次のセリフを再生する
        /// </summary>
        /// <param name="newData">次のセリフのデータ</param>
        async void SetNextDialogue(NovelData.ParagraphData.Dialogue newData)
        {
            //画像の変更
            _isImageChangeing = true;
            _imageCTS = new CancellationTokenSource();
            _isImageChangeing = !await _novelUI.SetNextImage(newData, _imageCTS.Token);

            _audioPlayer.SetSound(newData);

            //テキストを1文字ずつ再生
            _textCTS = new CancellationTokenSource();
            _isReading = true;
            _nextDialogueNum++;
            if (OnDialogueChanged != null)
                OnDialogueChanged(JsonUtility.FromJson<NovelData.ParagraphData.Dialogue>(JsonUtility.ToJson(newData)));
            _isReading = !await _novelUI.SetNextText(newData, _textCTS.Token);
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        /// <returns></returns>
        async void end()
        {
            //endが連続で呼ばれてしまうので、一回呼ばれたらフラグを立てておく
            if (_isEnd)
                return;
            _isEnd = true;
            if (_hideAfterPlay)
            {
                //UIをフェードアウト
                _isImageChangeing = true;
                _endFadeCTS = new CancellationTokenSource();
                _audioPlayer.AllStop();
                await _novelUI.FadeOut(_hideFadeTime, _endFadeCTS.Token);
                SetDisplay(false);
                _isImageChangeing = false;
            }

            if (OnEnd != null)
                OnEnd();
            _isPlaying = false;
        }

        /// <summary>
        /// 1文字ずつ表示していたテキストを全て表示する
        /// </summary>
        void FlashText()
        {
            _textCTS.Cancel();
            _novelUI.FlashText();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += _OnValidate;
        }

        private void _OnValidate()
        {
            UnityEditor.EditorApplication.delayCall -= _OnValidate;
            if (this == null) return;
            if (_audioPlayer != null)
            {
                _audioPlayer.SetSEVolume(_SEVolume);
                _audioPlayer.SetBGMVolume(_BGMVolume);
            }

            if (_novelUI == null)
            {
                GetComponent<CanvasGroup>().alpha = _isDisplay ? 1 : 0;
            }
            else
            {
                SetDisplay(_isDisplay);
            }

        }
#endif

        void OnDisable()
        {
            allCancel();
        }

        void OnDestroy()
        {
            allCancel();
        }

        void allCancel()
        {
            try
            {
                _audioPlayer.AllStop();
            }
            catch { }
            _textCTS.Cancel();
            _imageCTS.Cancel();
            _endFadeCTS.Cancel();
        }
        #endregion

    }


}

