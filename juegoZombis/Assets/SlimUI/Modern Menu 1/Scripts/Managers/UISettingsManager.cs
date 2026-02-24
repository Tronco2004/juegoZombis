using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

namespace SlimUI.ModernMenu{
	public class UISettingsManager : MonoBehaviour {

		public enum Platform {Desktop, Mobile};
		public Platform platform;
		// toggle buttons
		[Header("MOBILE SETTINGS")]
		public GameObject mobileSFXtext;
		public GameObject mobileMusictext;
		public GameObject mobileShadowofftextLINE;
		public GameObject mobileShadowlowtextLINE;
		public GameObject mobileShadowhightextLINE;

		[Header("VIDEO SETTINGS")]
		public GameObject fullscreentext;
		public GameObject ambientocclusiontext;
		public GameObject shadowofftextLINE;
		public GameObject shadowlowtextLINE;
		public GameObject shadowhightextLINE;
		public GameObject aaofftextLINE;
		public GameObject aa2xtextLINE;
		public GameObject aa4xtextLINE;
		public GameObject aa8xtextLINE;
		public GameObject vsynctext;
		public GameObject motionblurtext;
		public GameObject texturelowtextLINE;
		public GameObject texturemedtextLINE;
		public GameObject texturehightextLINE;
		public GameObject cameraeffectstext; 

		[Header("GAME SETTINGS")]
		public GameObject showhudtext;
		public GameObject tooltipstext;
		public GameObject difficultynormaltext;
		public GameObject difficultynormaltextLINE;
		public GameObject difficultyhardcoretext;
		public GameObject difficultyhardcoretextLINE;

		[Header("CONTROLS SETTINGS")]
		public GameObject invertmousetext;

		// sliders
		public GameObject musicSlider;
		public GameObject sensitivityXSlider;
		public GameObject sensitivityYSlider;
		public GameObject mouseSmoothSlider;

		private float sliderValue = 0.0f;
		private float sliderValueXSensitivity = 0.0f;
		private float sliderValueYSensitivity = 0.0f;
		private float sliderValueSmoothing = 0.0f;
		

		public void  Start (){
			// check difficulty
			if(PlayerPrefs.GetInt("NormalDifficulty") == 1){
				if(difficultynormaltextLINE != null) difficultynormaltextLINE.gameObject.SetActive(true);
				if(difficultyhardcoretextLINE != null) difficultyhardcoretextLINE.gameObject.SetActive(false);
			}
			else
			{
				if(difficultyhardcoretextLINE != null) difficultyhardcoretextLINE.gameObject.SetActive(true);
				if(difficultynormaltextLINE != null) difficultynormaltextLINE.gameObject.SetActive(false);
			}

			// check slider values
			if(musicSlider != null) musicSlider.GetComponent<Slider>().value = PlayerPrefs.GetFloat("MusicVolume");
			if(sensitivityXSlider != null) sensitivityXSlider.GetComponent<Slider>().value = PlayerPrefs.GetFloat("XSensitivity");
			if(sensitivityYSlider != null) sensitivityYSlider.GetComponent<Slider>().value = PlayerPrefs.GetFloat("YSensitivity");
			if(mouseSmoothSlider != null) mouseSmoothSlider.GetComponent<Slider>().value = PlayerPrefs.GetFloat("MouseSmoothing");

			// check full screen
			if(fullscreentext != null){
				if(Screen.fullScreen == true){
					fullscreentext.GetComponent<TMP_Text>().text = "on";
				}
				else if(Screen.fullScreen == false){
					fullscreentext.GetComponent<TMP_Text>().text = "off";
				}
			}

			// check hud value
			if(showhudtext != null){
				if(PlayerPrefs.GetInt("ShowHUD")==0){
					showhudtext.GetComponent<TMP_Text>().text = "off";
				}
				else{
					showhudtext.GetComponent<TMP_Text>().text = "on";
				}
			}

			// check tool tip value
			if(tooltipstext != null){
				if(PlayerPrefs.GetInt("ToolTips")==0){
					tooltipstext.GetComponent<TMP_Text>().text = "off";
				}
				else{
					tooltipstext.GetComponent<TMP_Text>().text = "on";
				}
			}

			// check shadow distance/enabled
			if(platform == Platform.Desktop){
				if(PlayerPrefs.GetInt("Shadows") == 0){
					QualitySettings.shadowCascades = 0;
					QualitySettings.shadowDistance = 0;
					if(shadowofftextLINE != null) shadowofftextLINE.gameObject.SetActive(true);
					if(shadowlowtextLINE != null) shadowlowtextLINE.gameObject.SetActive(false);
					if(shadowhightextLINE != null) shadowhightextLINE.gameObject.SetActive(false);
				}
				else if(PlayerPrefs.GetInt("Shadows") == 1){
					QualitySettings.shadowCascades = 2;
					QualitySettings.shadowDistance = 75;
					if(shadowofftextLINE != null) shadowofftextLINE.gameObject.SetActive(false);
					if(shadowlowtextLINE != null) shadowlowtextLINE.gameObject.SetActive(true);
					if(shadowhightextLINE != null) shadowhightextLINE.gameObject.SetActive(false);
				}
				else if(PlayerPrefs.GetInt("Shadows") == 2){
					QualitySettings.shadowCascades = 4;
					QualitySettings.shadowDistance = 500;
					if(shadowofftextLINE != null) shadowofftextLINE.gameObject.SetActive(false);
					if(shadowlowtextLINE != null) shadowlowtextLINE.gameObject.SetActive(false);
					if(shadowhightextLINE != null) shadowhightextLINE.gameObject.SetActive(true);
				}
			}else if(platform == Platform.Mobile){
				if(PlayerPrefs.GetInt("MobileShadows") == 0){
					QualitySettings.shadowCascades = 0;
					QualitySettings.shadowDistance = 0;
					if(mobileShadowofftextLINE != null) mobileShadowofftextLINE.gameObject.SetActive(true);
					if(mobileShadowlowtextLINE != null) mobileShadowlowtextLINE.gameObject.SetActive(false);
					if(mobileShadowhightextLINE != null) mobileShadowhightextLINE.gameObject.SetActive(false);
				}
				else if(PlayerPrefs.GetInt("MobileShadows") == 1){
					QualitySettings.shadowCascades = 2;
					QualitySettings.shadowDistance = 75;
					if(mobileShadowofftextLINE != null) mobileShadowofftextLINE.gameObject.SetActive(false);
					if(mobileShadowlowtextLINE != null) mobileShadowlowtextLINE.gameObject.SetActive(true);
					if(mobileShadowhightextLINE != null) mobileShadowhightextLINE.gameObject.SetActive(false);
				}
				else if(PlayerPrefs.GetInt("MobileShadows") == 2){
					QualitySettings.shadowCascades = 4;
					QualitySettings.shadowDistance = 100;
					if(mobileShadowofftextLINE != null) mobileShadowofftextLINE.gameObject.SetActive(false);
					if(mobileShadowlowtextLINE != null) mobileShadowlowtextLINE.gameObject.SetActive(false);
					if(mobileShadowhightextLINE != null) mobileShadowhightextLINE.gameObject.SetActive(true);
				}
			}


			// check vsync
			if(vsynctext != null){
				if(QualitySettings.vSyncCount == 0){
					vsynctext.GetComponent<TMP_Text>().text = "off";
				}
				else if(QualitySettings.vSyncCount == 1){
					vsynctext.GetComponent<TMP_Text>().text = "on";
				}
			}

			// check mouse inverse
			if(invertmousetext != null){
				if(PlayerPrefs.GetInt("Inverted")==0){
					invertmousetext.GetComponent<TMP_Text>().text = "off";
				}
				else if(PlayerPrefs.GetInt("Inverted")==1){
					invertmousetext.GetComponent<TMP_Text>().text = "on";
				}
			}

			// check motion blur
			if(motionblurtext != null){
				if(PlayerPrefs.GetInt("MotionBlur")==0){
					motionblurtext.GetComponent<TMP_Text>().text = "off";
				}
				else if(PlayerPrefs.GetInt("MotionBlur")==1){
					motionblurtext.GetComponent<TMP_Text>().text = "on";
				}
			}

			// check ambient occlusion
			if(ambientocclusiontext != null){
				if(PlayerPrefs.GetInt("AmbientOcclusion")==0){
					ambientocclusiontext.GetComponent<TMP_Text>().text = "off";
				}
				else if(PlayerPrefs.GetInt("AmbientOcclusion")==1){
					ambientocclusiontext.GetComponent<TMP_Text>().text = "on";
				}
			}

			// check texture quality
			if(PlayerPrefs.GetInt("Textures") == 0){
				QualitySettings.globalTextureMipmapLimit = 2;
				if(texturelowtextLINE != null) texturelowtextLINE.gameObject.SetActive(true);
				if(texturemedtextLINE != null) texturemedtextLINE.gameObject.SetActive(false);
				if(texturehightextLINE != null) texturehightextLINE.gameObject.SetActive(false);
			}
			else if(PlayerPrefs.GetInt("Textures") == 1){
				QualitySettings.globalTextureMipmapLimit = 1;
				if(texturelowtextLINE != null) texturelowtextLINE.gameObject.SetActive(false);
				if(texturemedtextLINE != null) texturemedtextLINE.gameObject.SetActive(true);
				if(texturehightextLINE != null) texturehightextLINE.gameObject.SetActive(false);
			}
			else if(PlayerPrefs.GetInt("Textures") == 2){
				QualitySettings.globalTextureMipmapLimit = 0;
				if(texturelowtextLINE != null) texturelowtextLINE.gameObject.SetActive(false);
				if(texturemedtextLINE != null) texturemedtextLINE.gameObject.SetActive(false);
				if(texturehightextLINE != null) texturehightextLINE.gameObject.SetActive(true);
			}
		}

		public void Update (){
			if(sensitivityXSlider != null) sliderValueXSensitivity = sensitivityXSlider.GetComponent<Slider>().value;
			if(sensitivityYSlider != null) sliderValueYSensitivity = sensitivityYSlider.GetComponent<Slider>().value;
			if(mouseSmoothSlider != null) sliderValueSmoothing = mouseSmoothSlider.GetComponent<Slider>().value;
		}

		public void FullScreen (){
			Screen.fullScreen = !Screen.fullScreen;

			if(Screen.fullScreen == true){
				fullscreentext.GetComponent<TMP_Text>().text = "on";
			}
			else if(Screen.fullScreen == false){
				fullscreentext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void MusicSlider (){
			//PlayerPrefs.SetFloat("MusicVolume", sliderValue);
			PlayerPrefs.SetFloat("MusicVolume", musicSlider.GetComponent<Slider>().value);
		}

		public void SensitivityXSlider (){
			PlayerPrefs.SetFloat("XSensitivity", sliderValueXSensitivity);
		}

		public void SensitivityYSlider (){
			PlayerPrefs.SetFloat("YSensitivity", sliderValueYSensitivity);
		}

		public void SensitivitySmoothing (){
			PlayerPrefs.SetFloat("MouseSmoothing", sliderValueSmoothing);
			Debug.Log(PlayerPrefs.GetFloat("MouseSmoothing"));
		}

		// the playerprefs variable that is checked to enable hud while in game
		public void ShowHUD (){
			if(PlayerPrefs.GetInt("ShowHUD")==0){
				PlayerPrefs.SetInt("ShowHUD",1);
				showhudtext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("ShowHUD")==1){
				PlayerPrefs.SetInt("ShowHUD",0);
				showhudtext.GetComponent<TMP_Text>().text = "off";
			}
		}

		// the playerprefs variable that is checked to enable mobile sfx while in game
		public void MobileSFXMute (){
			if(PlayerPrefs.GetInt("Mobile_MuteSfx")==0){
				PlayerPrefs.SetInt("Mobile_MuteSfx",1);
				mobileSFXtext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("Mobile_MuteSfx")==1){
				PlayerPrefs.SetInt("Mobile_MuteSfx",0);
				mobileSFXtext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void MobileMusicMute (){
			if(PlayerPrefs.GetInt("Mobile_MuteMusic")==0){
				PlayerPrefs.SetInt("Mobile_MuteMusic",1);
				mobileMusictext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("Mobile_MuteMusic")==1){
				PlayerPrefs.SetInt("Mobile_MuteMusic",0);
				mobileMusictext.GetComponent<TMP_Text>().text = "off";
			}
		}

		// show tool tips like: 'How to Play' control pop ups
		public void ToolTips (){
			if(PlayerPrefs.GetInt("ToolTips")==0){
				PlayerPrefs.SetInt("ToolTips",1);
				tooltipstext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("ToolTips")==1){
				PlayerPrefs.SetInt("ToolTips",0);
				tooltipstext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void NormalDifficulty (){
			if(difficultyhardcoretextLINE != null) difficultyhardcoretextLINE.gameObject.SetActive(false);
			if(difficultynormaltextLINE != null) difficultynormaltextLINE.gameObject.SetActive(true);
			PlayerPrefs.SetInt("NormalDifficulty",1);
			PlayerPrefs.SetInt("HardCoreDifficulty",0);
		}

		public void HardcoreDifficulty (){
			if(difficultyhardcoretextLINE != null) difficultyhardcoretextLINE.gameObject.SetActive(true);
			if(difficultynormaltextLINE != null) difficultynormaltextLINE.gameObject.SetActive(false);
			PlayerPrefs.SetInt("NormalDifficulty",0);
			PlayerPrefs.SetInt("HardCoreDifficulty",1);
		}

		public void ShadowsOff (){
			PlayerPrefs.SetInt("Shadows",0);
			QualitySettings.shadowCascades = 0;
			QualitySettings.shadowDistance = 0;
			shadowofftextLINE.gameObject.SetActive(true);
			shadowlowtextLINE.gameObject.SetActive(false);
			shadowhightextLINE.gameObject.SetActive(false);
		}

		public void ShadowsLow (){
			PlayerPrefs.SetInt("Shadows",1);
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 75;
			shadowofftextLINE.gameObject.SetActive(false);
			shadowlowtextLINE.gameObject.SetActive(true);
			shadowhightextLINE.gameObject.SetActive(false);
		}

		public void ShadowsHigh (){
			PlayerPrefs.SetInt("Shadows",2);
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 500;
			shadowofftextLINE.gameObject.SetActive(false);
			shadowlowtextLINE.gameObject.SetActive(false);
			shadowhightextLINE.gameObject.SetActive(true);
		}

		public void MobileShadowsOff (){
			PlayerPrefs.SetInt("MobileShadows",0);
			QualitySettings.shadowCascades = 0;
			QualitySettings.shadowDistance = 0;
			mobileShadowofftextLINE.gameObject.SetActive(true);
			mobileShadowlowtextLINE.gameObject.SetActive(false);
			mobileShadowhightextLINE.gameObject.SetActive(false);
		}

		public void MobileShadowsLow (){
			PlayerPrefs.SetInt("MobileShadows",1);
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 75;
			mobileShadowofftextLINE.gameObject.SetActive(false);
			mobileShadowlowtextLINE.gameObject.SetActive(true);
			mobileShadowhightextLINE.gameObject.SetActive(false);
		}

		public void MobileShadowsHigh (){
			PlayerPrefs.SetInt("MobileShadows",2);
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 500;
			mobileShadowofftextLINE.gameObject.SetActive(false);
			mobileShadowlowtextLINE.gameObject.SetActive(false);
			mobileShadowhightextLINE.gameObject.SetActive(true);
		}

		public void vsync (){
			if(QualitySettings.vSyncCount == 0){
				QualitySettings.vSyncCount = 1;
				vsynctext.GetComponent<TMP_Text>().text = "on";
			}
			else if(QualitySettings.vSyncCount == 1){
				QualitySettings.vSyncCount = 0;
				vsynctext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void InvertMouse (){
			if(PlayerPrefs.GetInt("Inverted")==0){
				PlayerPrefs.SetInt("Inverted",1);
				invertmousetext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("Inverted")==1){
				PlayerPrefs.SetInt("Inverted",0);
				invertmousetext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void MotionBlur (){
			if(PlayerPrefs.GetInt("MotionBlur")==0){
				PlayerPrefs.SetInt("MotionBlur",1);
				motionblurtext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("MotionBlur")==1){
				PlayerPrefs.SetInt("MotionBlur",0);
				motionblurtext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void AmbientOcclusion (){
			if(PlayerPrefs.GetInt("AmbientOcclusion")==0){
				PlayerPrefs.SetInt("AmbientOcclusion",1);
				ambientocclusiontext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("AmbientOcclusion")==1){
				PlayerPrefs.SetInt("AmbientOcclusion",0);
				ambientocclusiontext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void CameraEffects (){
			if(PlayerPrefs.GetInt("CameraEffects")==0){
				PlayerPrefs.SetInt("CameraEffects",1);
				cameraeffectstext.GetComponent<TMP_Text>().text = "on";
			}
			else if(PlayerPrefs.GetInt("CameraEffects")==1){
				PlayerPrefs.SetInt("CameraEffects",0);
				cameraeffectstext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void TexturesLow (){
			PlayerPrefs.SetInt("Textures",0);
			QualitySettings.globalTextureMipmapLimit = 2;
			texturelowtextLINE.gameObject.SetActive(true);
			texturemedtextLINE.gameObject.SetActive(false);
			texturehightextLINE.gameObject.SetActive(false);
		}

		public void TexturesMed (){
			PlayerPrefs.SetInt("Textures",1);
			QualitySettings.globalTextureMipmapLimit = 1;
			texturelowtextLINE.gameObject.SetActive(false);
			texturemedtextLINE.gameObject.SetActive(true);
			texturehightextLINE.gameObject.SetActive(false);
		}

		public void TexturesHigh (){
			PlayerPrefs.SetInt("Textures",2);
			QualitySettings.globalTextureMipmapLimit = 0;
			texturelowtextLINE.gameObject.SetActive(false);
			texturemedtextLINE.gameObject.SetActive(false);
			texturehightextLINE.gameObject.SetActive(true);
		}
	}
}