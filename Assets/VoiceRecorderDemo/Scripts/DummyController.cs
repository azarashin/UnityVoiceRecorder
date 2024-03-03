using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Voish.VoiceRecorder;

public class DummyController : MonoBehaviour
{
    [SerializeField]
    int _maxDuration = 30;

    [SerializeField]
    VoiceRecorderManager _voiceRecorderManager;

    [SerializeField]
    Button _buttonSetup;

    [SerializeField]
    Button _buttonStartRecord;

    [SerializeField]
    Button _buttonFinishRecord;

    [SerializeField]
    Button _buttonTerm;

    [SerializeField]
    Button _buttonLoadPlay;

    [SerializeField]
    AudioSource _audioSourceToPlaySound;

    private bool _isRecording = false;

    // Start is called before the first frame update
    void Start()
    {
        _buttonSetup.interactable = true;
        _buttonStartRecord.interactable = false;
        _buttonFinishRecord.interactable = false;
        _buttonTerm.interactable = false;
        _buttonLoadPlay.interactable = true;
    }


    // Update is called once per frame
    void Update()
    {
        if(_isRecording)
        {
            if(!_voiceRecorderManager.IsRecording)
            {
                OnFinishRecord(); 
            }
        }
        
    }


    public void OnSetup()
    {
        StartCoroutine(CoSetup());
    }

    private IEnumerator CoSetup()
    {
        yield return _voiceRecorderManager.CoSetup();
        _voiceRecorderManager.PlayViewer();

        _buttonSetup.interactable = false;
        _buttonStartRecord.interactable = true;
        _buttonFinishRecord.interactable = false;
        _buttonTerm.interactable = false;
        _buttonLoadPlay.interactable = false;
    }

    public void OnStartRecord()
    {
        StartCoroutine(CoStartRecord());
    }

    private IEnumerator CoStartRecord()
    {
        _voiceRecorderManager.StopViewer();
        yield return _voiceRecorderManager.CoStartRecord(_maxDuration);

        _isRecording = true; 

        _buttonSetup.interactable = false;
        _buttonStartRecord.interactable = false;
        _buttonFinishRecord.interactable = true;
        _buttonTerm.interactable = false;
        _buttonLoadPlay.interactable = false;
    }


    public void OnFinishRecord()
    {
        StartCoroutine(CoFinishRecord()); 
    }

    private IEnumerator CoFinishRecord()
    {
        _voiceRecorderManager.StopViewer();
        yield return _voiceRecorderManager.CoFinishRecord("test.wav");
        _voiceRecorderManager.PlayViewer();

        _isRecording = false; 

        _buttonSetup.interactable = false;
        _buttonStartRecord.interactable = false;
        _buttonFinishRecord.interactable = false;
        _buttonTerm.interactable = true;
        _buttonLoadPlay.interactable = false;
    }

    public void OnTerm()
    {
        StartCoroutine(CoTerm());
    }

    private IEnumerator CoTerm()
    {
        _voiceRecorderManager.StopViewer();
        yield return _voiceRecorderManager.CoFinishRecord(null);

        _buttonSetup.interactable = true;
        _buttonStartRecord.interactable = false;
        _buttonFinishRecord.interactable = false;
        _buttonTerm.interactable = false;
        _buttonLoadPlay.interactable = true;
    }

    public void OnLoadAndPlaySound()
    {
        StartCoroutine(CoLoadAndPlaySound());
    }

    private IEnumerator CoLoadAndPlaySound()
    {
        _buttonSetup.interactable = false;
        _buttonStartRecord.interactable = false;
        _buttonFinishRecord.interactable = false;
        _buttonTerm.interactable = false;
        _buttonLoadPlay.interactable = false;

        yield return _voiceRecorderManager.CoLoadSound("test.wav", _audioSourceToPlaySound);
        if(_audioSourceToPlaySound.clip != null)
        {
            _audioSourceToPlaySound.Play();
            while(_audioSourceToPlaySound.isPlaying)
            {
                yield return null; 
            }
        }

        _buttonSetup.interactable = true;
        _buttonStartRecord.interactable = false;
        _buttonFinishRecord.interactable = false;
        _buttonTerm.interactable = false;
        _buttonLoadPlay.interactable = true;
    }
}
