using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FourHandsTwoCats.VideoPlayer
{
    public class VideoPlayerManager : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Video.VideoPlayer videoPlayer;
        [Space]
        [SerializeField] private Slider videoSlider;
        [SerializeField] private TextMeshProUGUI frameCountText;
        [Space]
        [SerializeField] private Button speedChangeButton;
        [SerializeField] private TextMeshProUGUI speedAmount;
        [SerializeField] private List<float> possiblePlaybackSpeed;
        [Space]
        [SerializeField] private bool playOnAwake;
        [Space]
        [SerializeField] private Image muteImage;
        [SerializeField] private Sprite muteSprite;
        [SerializeField] private Sprite unmuteSprite;
        [Header("Play Pause Button")]
        [SerializeField] private Image playPauseImage;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite pauseSprite;
  
        private bool _isReady;
        private bool _sliderMode;
        private string _frameCountString = "Frame : {0}/{1}";
        private VideoPlayerInputs _inputs;
        private int _currentPlaybackSpeedIndex;

        private void Awake()
        {
            _inputs = new VideoPlayerInputs();
            _inputs.VideoPlayerControl.NextFrame.performed += ctx => Nextframe();
            _inputs.VideoPlayerControl.PreviousFrame.performed += ctx => PreviousFrame();
            _inputs.VideoPlayerControl.PlayPause.performed += ctx =>
            {
                if (videoPlayer.isPlaying) PauseVideo();
                else PlayVideo();
            };
            _inputs.Enable();

            _currentPlaybackSpeedIndex = 0;
            speedChangeButton.onClick.AddListener(() => HandleSpeedChange());
            _isReady = playOnAwake;
            videoPlayer.Prepare();
            videoPlayer.frame = 0;
           
        }
        public void TogglePlayPause()
        {
            bool isPlaying = videoPlayer.isPlaying;

            if (isPlaying)
                videoPlayer.Pause();
            else
                videoPlayer.Play();

            playPauseImage.sprite =
                isPlaying ? playSprite : pauseSprite;
        }
        private void HandleSpeedChange()
        {
            _currentPlaybackSpeedIndex = (_currentPlaybackSpeedIndex + 1) % (possiblePlaybackSpeed.Count);
            speedAmount.text = "x" + possiblePlaybackSpeed[_currentPlaybackSpeedIndex];
            videoPlayer.playbackSpeed = possiblePlaybackSpeed[_currentPlaybackSpeedIndex];
            videoPlayer.Play();
        }

        private void OnDisable()
        {
            _inputs.Disable();
        }

        public void LoadVideo(string pickedFile)
        {
            _isReady = false;
            videoPlayer.Pause();
            videoPlayer.url = pickedFile;
            videoPlayer.prepareCompleted += EnableFonctionnalities;
            videoPlayer.Prepare();
        }

        private void EnableFonctionnalities(UnityEngine.Video.VideoPlayer source)
        {
            _isReady = true;

            videoSlider.value = 0;
            videoSlider.maxValue = 1;

            videoPlayer.frame = 0;

            playPauseImage.sprite = pauseSprite;
        }

        private void LateUpdate()
        {
            if (_isReady)
            {
                if (!_sliderMode)
                    videoSlider.value = (float)videoPlayer.frame / (float)videoPlayer.frameCount;

                frameCountText.text = string.Format(_frameCountString, videoPlayer.frame, videoPlayer.frameCount);
            }

        }

        public void OnUsingSlider()
        {
            videoSlider.onValueChanged.AddListener((value) =>
            {
                if (_isReady)
                {
                    long targetFrame = (long)(value * (float)videoPlayer.frameCount);
                    videoPlayer.frame = targetFrame;
                }
            });

            PauseVideo();
            _sliderMode = true;
        }

        public void OnStopUsingSlider()
        {
            videoSlider.onValueChanged.RemoveAllListeners();
            _sliderMode = false;
        }

        public void PlayVideo()
        {
            if (_isReady)
            {
                videoPlayer.Play();
            }
        }

        public void PauseVideo()
        {
            if (_isReady)
            {
                videoPlayer.Pause();
            }
        }

        public void Nextframe()
        {
            PauseVideo();
            if (_isReady)
            {
                videoPlayer.StepForward();
            }
        }

        public void PreviousFrame()
        {
            PauseVideo();
            if (_isReady)
            {
                long targetFrame = videoPlayer.frame - 1;
                if (targetFrame < 0) targetFrame = 0;
                videoPlayer.frame = targetFrame;
            }
        }

        public void ToggleMute()
        {
            bool isMute = !videoPlayer.GetDirectAudioMute(0);
            videoPlayer.SetDirectAudioMute(0, isMute);
            muteImage.sprite = isMute ? muteSprite : unmuteSprite;
        }

        public float GetFrameRate() => videoPlayer.frameRate;

        public long GetCurrentFrame() => videoPlayer.frame;
    }
}


