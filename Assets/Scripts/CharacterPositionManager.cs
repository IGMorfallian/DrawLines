using System;
using System.Collections.Generic;
using Dreamteck.Splines.Primitives;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class CharacterPositionManager : MonoBehaviour
{
    [SerializeField] private Character _characterPrefab;
    [SerializeField] private Curve _curve;
    [SerializeField] private int _startCharacterCount;
    [FormerlySerializedAs("_additionalCamera")] [SerializeField] private Camera _camera;

    public List<Character> _characters = new List<Character>();

    private void Awake()
    {
        for (int i = 0; i < _startCharacterCount; i++)
        {
            var character = Instantiate(_characterPrefab, transform );
            character.onAddNewCharacter += onAddNewCharacter;
            character.onDestroyCharacter += CharacterDestroy;
            character.InCrowd = true;
            _characters.Add(character);
        }
    }
    
    private void OnEnable()
    {
        _curve.OnCurveUpdate += CharactersPositionUpdate;
    }
    

    private void OnDisable()
    {
        _curve.OnCurveUpdate -= CharactersPositionUpdate;
    }

    private void onAddNewCharacter(Character character)
    {
        _characters.Add(character);
        character.InCrowd = true;
        character.transform.SetParent(gameObject.transform);
        character.onAddNewCharacter += onAddNewCharacter;
        character.onDestroyCharacter += CharacterDestroy;
        CharactersPositionUpdate();
    }

    private void CharacterDestroy(Character obj)
    {
        obj.onDestroyCharacter -= CharacterDestroy;
        _characters.Remove(obj);
        if (_characters.Count == 0)
        {
            Time.timeScale = 0;
            Debug.Log("You lose");
            
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void CharactersPositionUpdate()
    {
        var normalizedDistanceBetweenCharacters = 1f / _characters.Count;
        for (int i = 0; i < _characters.Count; i++)
        {
            var distanceToCharacter = normalizedDistanceBetweenCharacters * i;
            if (i == 0)
            {
                distanceToCharacter += normalizedDistanceBetweenCharacters / 2;
            }

            var screenPosition = _curve.GetScreenPositionFromNormalizedValue(distanceToCharacter);

            var worldPoint = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, _camera.transform.position.y));
            var localPoint = _camera.transform.InverseTransformPoint(worldPoint);
            _characters[i].transform.localPosition = new Vector3(localPoint.x, 0, localPoint.y) ;
            if (i > 0)
            {
                _characters[i].transform.rotation = _characters[i - 1].transform.rotation;
            }
        }
    }
}