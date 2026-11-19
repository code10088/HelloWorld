package com.qs.helloworld.sdk;

import android.app.Activity;

import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialCancellationException;
import androidx.credentials.exceptions.GetCredentialException;

import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

public class GoogleSDK {

    // Web OAuth client ID, also the aud of the returned ID token
    public static final String WEB_CLIENT_ID = "33980766200-3i6g7egcillmld5n6phmf6li4bh47aru.apps.googleusercontent.com";

    public static void Login(Activity activity) {
        GetCredentialRequest request = new GetCredentialRequest.Builder()
                .addCredentialOption(new GetSignInWithGoogleOption.Builder(WEB_CLIENT_ID).build())
                .build();

        CredentialManager.create(activity).getCredentialAsync(activity, request, null,
                activity::runOnUiThread,
                new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                    @Override
                    public void onResult(GetCredentialResponse response) {
                        GoogleIdTokenCredential credential =
                                GoogleIdTokenCredential.createFrom(response.getCredential().getData());
                        UnityActivity.CallUnity("GoogleLoginCallback", "0|" + credential.getIdToken());
                    }

                    @Override
                    public void onError(GetCredentialException e) {
                        if (e instanceof GetCredentialCancellationException) {
                            UnityActivity.CallUnity("GoogleLoginCallback", "-3");
                        } else {
                            UnityActivity.CallUnity("GoogleLoginCallback",
                                    "-4|" + e.getType() + "|" + e.getMessage());
                        }
                    }
                });
    }
}