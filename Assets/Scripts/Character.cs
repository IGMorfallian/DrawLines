using System;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField] private GameObject particle;
    public event Action<Character> onAddNewCharacter;
    public event Action<Character> onDestroyCharacter;
    
    public bool InCrowd { get; set; } = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Character>(out var character) && !character.InCrowd)
        {
            onAddNewCharacter.Invoke(character);
        }

        if ((other.TryGetComponent<Obstacles>(out var obstacles)))
        {
            TryGetComponent<Character>(out var characterToDestroy);
            onDestroyCharacter.Invoke(characterToDestroy);
            var effect = Instantiate(particle, gameObject.transform.position, transform.rotation);
            Destroy(gameObject);
            Destroy(effect, 5.0f);
        }
        if ((other.TryGetComponent<Finish>(out var finish)))
        {
            Time.timeScale = 0;
            
            Debug.Log("U win");
        }
    }
}