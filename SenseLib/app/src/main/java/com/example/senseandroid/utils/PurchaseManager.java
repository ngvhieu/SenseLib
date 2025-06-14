// Callbacks
public interface SuccessCallback<T> {
    void onSuccess(T result);
}

public interface ErrorCallback {
    void onError(String message);
}

public interface PurchaseCallback<T> {
    void onSuccess(T result);

    void onError(String message);
}

public void initialize(SuccessCallback<Boolean> callback) {
    if (isInitialized) {
        if (callback != null) {
            callback.onSuccess(true);
        }
        return;
    }
    syncWithServer(new PurchaseCallback<Boolean>() {
        @Override
        public void onSuccess(Boolean success) {
            isInitialized = true;
            if (callback != null) {
                callback.onSuccess(success);
            }
        }

        @Override
        public void onError(String message) {
            Log.e(TAG, "Lỗi khởi tạo: " + message);
            if (callback != null) {
                callback.onSuccess(false);
            }
        }
    });
}