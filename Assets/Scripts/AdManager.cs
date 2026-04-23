using UnityEngine;
using System.Collections;
using System;

/* 
 * IMPORTANT: You must import the Google Mobile Ads Unity Plugin to use this script.
 * 1. Download the latest .unitypackage from: https://github.com/googleads/googleads-mobile-unity/releases
 * 2. Import it into your project.
 * 3. Go to Project Settings -> Player -> Scripting Define Symbols and add "USE_ADMOB".
 */

#if USE_ADMOB
using GoogleMobileAds.Api;
#endif

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [Header("AdMob Settings")]
    [SerializeField] private bool _useTestAds = true;
    [Tooltip("Enter your AdMob App ID here (for reference, settings are in GoogleMobileAds menu)")]
    [SerializeField] private string _appId = "ca-app-pub-6197603164598979~4759351749";

    [Header("Android Ad Unit IDs")]
    [SerializeField] private string _bannerAdUnitId = "ca-app-pub-6197603164598979/6751090262";
    [SerializeField] private string _interstitialAdUnitId = "ca-app-pub-6197603164598979/4029243529";

    // Google's official Test IDs
    private const string TestBannerId = "ca-app-pub-3940256099942544/6300978111";
    private const string TestInterstitialId = "ca-app-pub-3940256099942544/1033173712";

    public string CurrentBannerId => _useTestAds ? TestBannerId : _bannerAdUnitId;
    public string CurrentInterstitialId => _useTestAds ? TestInterstitialId : _interstitialAdUnitId;

    #if USE_ADMOB
    private BannerView _bannerView;
    private InterstitialAd _interstitialAd;
    #endif

    private float _nextInterstitialTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
    #if USE_ADMOB
        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize((InitializationStatus status) =>
        {
            Debug.Log($"AdMob Initialized. Test Mode: {_useTestAds}");
            RequestBanner();
            RequestAndLoadInterstitialAd();
            SetNextInterstitialTimer();
        });
    #else
        Debug.LogWarning("AdMob is not enabled. Please import the Google Mobile Ads plugin and add 'USE_ADMOB' to Scripting Define Symbols.");
        SetNextInterstitialTimer(); // Still set timer for mock logs
    #endif
    }

    private void Update()
    {
        if (Time.time >= _nextInterstitialTime)
        {
            ShowInterstitial();
        }
    }

    public void RequestBanner()
    {
    #if USE_ADMOB
        string id = CurrentBannerId;
        Debug.Log($"Requesting Banner Ad ({(_useTestAds ? "TEST" : "REAL")}): {id}");
        
        if (_bannerView != null) _bannerView.Destroy();
        _bannerView = new BannerView(id, AdSize.Banner, AdPosition.Bottom);
        
        // Register for banner events
        _bannerView.OnBannerAdLoaded += () => { Debug.Log("Banner Ad Loaded successfully."); };
        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) => { Debug.LogError("Banner Ad Failed to load: " + error); };

        AdRequest adRequest = new AdRequest();
        _bannerView.LoadAd(adRequest);
    #else
        Debug.Log("[Mock AdMob] Banner Requested.");
    #endif
    }

    public void RequestAndLoadInterstitialAd()
    {
    #if USE_ADMOB
        string id = CurrentInterstitialId;
        Debug.Log($"Requesting Interstitial Ad ({(_useTestAds ? "TEST" : "REAL")}): {id}");
        
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        var adRequest = new AdRequest();
        InterstitialAd.Load(id, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("Interstitial ad failed to load: " + error);
                    return;
                }
                Debug.Log("Interstitial Ad Loaded successfully.");
                _interstitialAd = ad;
                RegisterEventHandlers(ad);
            });
    #else
        Debug.Log("[Mock AdMob] Interstitial Requested.");
    #endif
    }

    public void ShowInterstitial()
    {
#if USE_ADMOB
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            _interstitialAd.Show();
            SetNextInterstitialTimer();
        }
        else
        {
            // If not ready, check again soon
            _nextInterstitialTime = Time.time + 30f;
        }
#else
        Debug.Log("[Mock AdMob] Interstitial Shown.");
        SetNextInterstitialTimer();
#endif
    }

#if USE_ADMOB
    private void RegisterEventHandlers(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () => { RequestAndLoadInterstitialAd(); };
        ad.OnAdFullScreenContentFailed += (AdError error) => { RequestAndLoadInterstitialAd(); };
    }
#endif

    private void SetNextInterstitialTimer()
    {
        float delay = UnityEngine.Random.Range(180f, 240f); // 3-4 minutes
        _nextInterstitialTime = Time.time + delay;
        Debug.Log($"Next Interstitial in {delay:F0} seconds.");
    }
}
