using UnityEngine;

public class OpenLinkButton : MonoBehaviour
{
    public void OpenFacebook()
    {
        Application.OpenURL("https://www.facebook.com/nguyen.pham.104600");
    }

    public void OpenGitHub()
    {
        Application.OpenURL("https://github.com/Chanhtin227/Flying-Battle.git");
    }
}