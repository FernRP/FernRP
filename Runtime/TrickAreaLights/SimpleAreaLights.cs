using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("FernRender/Simple Area Light")]
[ExecuteInEditMode]
[RequireComponent (typeof (Light))]
public class SimpleAreaLights : MonoBehaviour
{

	[Header("Area Light Properties")]
	[Range(0,40)]
	public float lightLength = 1.0f;
//TODO: why do we have to set a min radius to get specular highlights?
	[Range(0.04f,40f)]
	public float lightRadius = 0.5f;
	[Range(0,1)]
	public float specularIntensity = 1.0f;

	[Header("Light Overwrites")]
	public Color lightColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
	[Range(0,8)]
	public float lightIntensity = 1.0f;
	
	private Light lightSource;
	private float range;

    
#if UNITY_EDITOR
	void Update () {
		UpdateAreaLight();
	}
#endif

	void OnValidate() {
		UpdateAreaLight();	
	}

	void OnEnable() {
		UpdateAreaLight();
	}

	void OnDisable() {
		ResetAreaLight();
	}
	
	public void UpdateAreaLight () {
		if (lightSource == null) {
			lightSource = GetComponent<Light>();
		}
		
		// Adjust light range to light length
		range = lightSource.range;
		if (range < lightLength * 0.75f) {
			range = lightLength * 0.75f;
			lightSource.range = range;
		}

		var tweakedLightCol = lightColor;
		// Bake light intensity into color
		tweakedLightCol *= lightIntensity;

		// Bring radius and length into a 0-1 range
		float radiusOverRange = lightRadius / 80.0f;
		float halfLengthOverRange = lightLength / 80.0f;

		// http://stackoverflow.com/questions/17638800/storing-two-float-values-in-a-single-float-variable
		tweakedLightCol.a = Mathf.Floor(radiusOverRange * 2047.0f) * 2048.0f + Mathf.Floor(halfLengthOverRange* 2047.0f) + specularIntensity * 0.5f;

		// Make sure that the light's intensity is set to 1.0 as otherwise our alpha value will by multipied by it
		lightSource.intensity = 1.0f;
		lightSource.color = tweakedLightCol;
	}

#if UNITY_EDITOR
	
	void OnDrawGizmosSelected()
    {
	//	Set matrix
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.color = Color.red;
		if (lightLength == 0.0) {
			Gizmos.DrawWireSphere(Vector3.zero, lightRadius);
		}
		else {
			Gizmos.DrawWireSphere(Vector3.zero - new Vector3(0, lightLength * 0.5f, 0), lightRadius);
			Gizmos.DrawWireSphere(Vector3.zero + new Vector3(0, lightLength * 0.5f, 0), lightRadius);
		}
    //	Reset matrix
        Gizmos.matrix = Matrix4x4.identity; 
    }
#endif

	void ResetAreaLight() {
		if (lightSource == null) {
			lightSource = GetComponent<Light>();
		}
		lightSource.color	= new Color(lightColor.r, lightColor.g, lightColor.b, 1.0f);
		lightSource.intensity = lightIntensity;
	}
}
