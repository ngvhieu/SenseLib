/**
 * Document Text-to-Speech Component
 * Thêm chức năng đọc tài liệu vào trang xem tài liệu
 */
class DocumentTTS {
    constructor(documentId, container, options = {}) {
        this.documentId = documentId;
        this.container = container;
        this.options = {
            languageCode: options.languageCode || 'vi-VN',
            voiceName: options.voiceName || 'vi-VN-Standard-A',
            buttonClass: options.buttonClass || 'btn btn-primary',
            iconClass: options.iconClass || 'fas fa-headphones',
            buttonText: options.buttonText || 'Đọc tài liệu',
            loadingText: options.loadingText || 'Đang xử lý...',
            errorText: options.errorText || 'Đã xảy ra lỗi',
            darkMode: options.darkMode || false
        };

        this.audioFiles = [];
        this.audioPlayers = [];
        this.isPlaying = false;
        this.currentPlayerIndex = 0;

        // Danh sách giọng đọc hỗ trợ
        this.voices = {
            'vi-VN': [
                { id: 'vi-VN-Standard-A', name: 'Nữ 1', gender: 'female' },
                { id: 'vi-VN-Standard-B', name: 'Nam 1', gender: 'male' },
                { id: 'vi-VN-Standard-C', name: 'Nữ 2', gender: 'female' },
                { id: 'vi-VN-Standard-D', name: 'Nam 2', gender: 'male' }
            ],
            'en-US': [
                { id: 'en-US-Standard-A', name: 'Nam A', gender: 'male' },
                { id: 'en-US-Standard-B', name: 'Nam B', gender: 'male' },
                { id: 'en-US-Standard-C', name: 'Nữ C', gender: 'female' },
                { id: 'en-US-Standard-E', name: 'Nữ E', gender: 'female' },
                { id: 'en-US-Standard-F', name: 'Nữ F', gender: 'female' }
            ]
        };

        // Danh sách ngôn ngữ hỗ trợ
        this.languages = [
            { code: 'vi-VN', name: 'Tiếng Việt' },
            { code: 'en-US', name: 'Tiếng Anh (Mỹ)' }
        ];

        this.init();
    }

    /**
     * Khởi tạo component
     */
    init() {
        // Tạo container chính
        this.mainContainer = document.createElement('div');
        this.mainContainer.className = 'tts-component';

        // Tạo nút và container đơn giản hơn
        this.mainContainer.innerHTML = `
            <div class="tts-main-button-container">
                <button type="button" class="tts-main-button ${this.options.buttonClass} w-100 py-2">
                    <i class="${this.options.iconClass} me-2"></i> ${this.options.buttonText}
                </button>
            </div>
            <div class="tts-settings" style="display: none;">
                <div class="card mt-2 shadow-sm border-0 rounded-3 animate__animated animate__fadeIn">
                    <div class="card-header bg-primary text-white d-flex justify-content-between align-items-center">
                        <h6 class="mb-0"><i class="fas fa-sliders-h me-2"></i> Tùy chọn giọng đọc</h6>
                        <button type="button" class="btn-close btn-close-white tts-close-settings" aria-label="Đóng"></button>
                    </div>
                    <div class="card-body">
                        <div class="row g-3">
                            <div class="col-md-6">
                                <label for="tts-language-select" class="form-label">Ngôn ngữ:</label>
                                <div class="input-group">
                                    <span class="input-group-text"><i class="fas fa-language"></i></span>
                                    <select id="tts-language-select" class="form-select">
                                        ${this.languages.map(lang =>
            `<option value="${lang.code}" ${lang.code === this.options.languageCode ? 'selected' : ''}>
                                                ${lang.name}
                                            </option>`
        ).join('')}
                                    </select>
                                </div>
                            </div>
                            <div class="col-md-6">
                                <label for="tts-voice-select" class="form-label">Giọng đọc:</label>
                                <div class="input-group">
                                    <span class="input-group-text"><i class="fas fa-microphone"></i></span>
                                    <select id="tts-voice-select" class="form-select"></select>
                                </div>
                            </div>
                        </div>
                        
                        <div class="d-grid gap-2 mt-3">
                            <button type="button" class="btn btn-primary convert-button">
                                <i class="fas fa-play me-2"></i> Chuyển đổi và nghe
                            </button>
                        </div>
                    </div>
                </div>
            </div>
            <div class="tts-audio-container mt-3 animate__animated animate__fadeIn" style="display: none;"></div>
            <div class="tts-loading mt-3 text-center animate__animated animate__pulse" style="display: none;">
                <div class="spinner-grow text-primary" role="status">
                    <span class="visually-hidden">Đang xử lý...</span>
                </div>
                <p class="mt-2 text-muted">${this.options.loadingText}</p>
            </div>
            <div class="tts-error alert alert-danger mt-3 small py-2 animate__animated animate__headShake" style="display: none;"></div>
        `;
        this.container.appendChild(this.mainContainer);

        // Lưu trữ các tham chiếu đến các phần tử
        this.ttsButton = this.mainContainer.querySelector('.tts-main-button');
        this.settingsPanel = this.mainContainer.querySelector('.tts-settings');
        this.closeSettingsButton = this.mainContainer.querySelector('.tts-close-settings');
        this.convertButton = this.mainContainer.querySelector('.convert-button');
        this.audioContainer = this.mainContainer.querySelector('.tts-audio-container');
        this.loadingContainer = this.mainContainer.querySelector('.tts-loading');
        this.errorContainer = this.mainContainer.querySelector('.tts-error');
        this.languageSelect = this.mainContainer.querySelector('#tts-language-select');
        this.voiceSelect = this.mainContainer.querySelector('#tts-voice-select');

        // Cập nhật danh sách giọng đọc
        this.updateVoicesList();

        // Đăng ký sự kiện
        this.ttsButton.addEventListener('click', (e) => {
            // Kiểm tra xem đã có audio hay chưa
            if (this.audioFiles.length > 0) {
                // Nếu đã có audio, hiển thị/ẩn audio player
                this.toggleAudioContainer();
            } else {
                // Hiển thị settings panel trước, chuyển đổi khi người dùng muốn nghe
                this.toggleSettings();
            }
            e.stopPropagation();
        });

        // Thêm sự kiện đóng settings
        this.closeSettingsButton.addEventListener('click', () => this.toggleSettings());

        // Sự kiện chuyển đổi
        this.convertButton.addEventListener('click', () => this.convertToSpeech());

        this.languageSelect.addEventListener('change', () => this.handleLanguageChange());

        // Nếu sử dụng dark mode
        if (this.options.darkMode) {
            this.mainContainer.querySelector('.card').classList.add('bg-dark', 'text-light');
            this.mainContainer.querySelector('.card-header').classList.replace('bg-primary', 'bg-info');
        }

        // Thêm stylesheet
        this.addStylesheet();
    }

    /**
     * Thêm stylesheet cho component
     */
    addStylesheet() {
        const style = document.createElement('style');
        style.textContent = `
            .tts-component {
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            }
            
            .tts-main-button {
                transition: all 0.3s ease;
                border-radius: 50px;
                position: relative;
                overflow: hidden;
                box-shadow: 0 3px 10px rgba(0,0,0,0.1);
            }
            
            .tts-main-button:hover {
                transform: translateY(-2px);
                box-shadow: 0 5px 15px rgba(0,0,0,0.15);
            }
            
            .tts-main-button:active {
                transform: translateY(0);
            }
            
            .tts-audio-part {
                border-radius: 10px;
                overflow: hidden;
                transition: all 0.3s ease;
                border: 1px solid #eaeaea;
                margin-bottom: 1rem;
            }
            
            .tts-audio-part.active-audio {
                border-color: #007bff;
                box-shadow: 0 0 15px rgba(0, 123, 255, 0.25);
                background-color: #f0f7ff;
            }
            
            .tts-audio-part.active-audio .card-header {
                background-color: #e7f1ff;
                font-weight: bold;
                color: #007bff;
            }
            
            .tts-audio-part .card-header {
                padding: 0.75rem 1rem;
                background-color: #f8f9fa;
                border-bottom: 1px solid #eaeaea;
            }
            
            .tts-audio-part .card-body {
                padding: 0.75rem;
            }
            
            .tts-audio-part audio {
                border-radius: 8px;
                background-color: #f8f9fa;
            }
            
            .tts-controls-panel {
                position: sticky;
                top: 0;
                z-index: 1020;
                background-color: #ffffff;
                border-radius: 10px;
                padding: 1rem;
                margin-bottom: 1.5rem;
                box-shadow: 0 5px 15px rgba(0, 0, 0, 0.05);
                display: flex;
                flex-wrap: wrap;
                gap: 0.5rem;
                align-items: center;
            }
            
            .tts-controls-panel button {
                border-radius: 50px;
                padding: 0.5rem 1rem;
                display: flex;
                align-items: center;
                gap: 0.5rem;
                box-shadow: 0 2px 5px rgba(0,0,0,0.1);
                transition: all 0.2s ease;
            }
            
            .tts-controls-panel button:hover {
                transform: translateY(-2px);
                box-shadow: 0 5px 10px rgba(0,0,0,0.15);
            }
            
            .tts-progress-container {
                height: 6px;
                background-color: #e9ecef;
                border-radius: 3px;
                overflow: hidden;
                margin: 0.5rem 0;
                width: 100%;
            }
            
            .tts-progress-bar {
                height: 100%;
                background-color: #007bff;
                border-radius: 3px;
                transition: width 0.2s ease;
            }
            
            .tts-time-display {
                font-family: monospace;
                font-size: 0.9rem;
                color: #6c757d;
                margin-left: auto;
                white-space: nowrap;
            }
            
            .tts-audio-title {
                display: flex;
                align-items: center;
                gap: 0.5rem;
                margin-bottom: 1rem;
            }
            
            .tts-audio-title h6 {
                margin: 0;
                font-weight: 600;
            }
            
            @media (max-width: 768px) {
                .tts-controls-panel {
                    padding: 0.75rem;
                    justify-content: center;
                }
                
                .tts-time-display {
                    width: 100%;
                    text-align: center;
                    margin: 0.5rem 0 0;
                }
            }
            
            .audio-part-header {
                display: flex;
                align-items: center;
                justify-content: space-between;
            }
            
            .audio-part-title {
                display: flex;
                align-items: center;
                gap: 0.5rem;
            }
            
            .audio-part-actions {
                display: flex;
                gap: 0.5rem;
            }
            
            .audio-part-actions button {
                background: none;
                border: none;
                padding: 0.25rem;
                cursor: pointer;
                color: #6c757d;
                transition: color 0.2s ease;
            }
            
            .audio-part-actions button:hover {
                color: #007bff;
            }
            
            .audio-duration {
                font-size: 0.8rem;
                color: #6c757d;
            }
            
            /* Animate.css classes */
            .animate__animated {
                animation-duration: 0.5s;
            }
            
            .animate__fadeIn {
                animation-name: fadeIn;
            }
            
            .animate__pulse {
                animation-name: pulse;
                animation-iteration-count: infinite;
            }
            
            .animate__headShake {
                animation-name: headShake;
            }
            
            @keyframes fadeIn {
                from { opacity: 0; }
                to { opacity: 1; }
            }
            
            @keyframes pulse {
                0% { transform: scale(1); }
                50% { transform: scale(1.05); }
                100% { transform: scale(1); }
            }
            
            @keyframes headShake {
                0% { transform: translateX(0); }
                6.5% { transform: translateX(-6px); }
                18.5% { transform: translateX(5px); }
                31.5% { transform: translateX(-3px); }
                43.5% { transform: translateX(2px); }
                50% { transform: translateX(0); }
            }
        `;
        document.head.appendChild(style);
    }

    /**
     * Cập nhật danh sách giọng đọc dựa trên ngôn ngữ đã chọn
     */
    updateVoicesList() {
        const language = this.languageSelect.value;
        const voices = this.voices[language] || [];

        this.voiceSelect.innerHTML = voices.map(voice =>
            `<option value="${voice.id}" ${voice.id === this.options.voiceName ? 'selected' : ''}>
                ${voice.name} (${voice.gender === 'female' ? 'Nữ' : 'Nam'})
             </option>`
        ).join('');

        // Cập nhật giọng đọc hiện tại
        this.options.languageCode = language;
        this.options.voiceName = this.voiceSelect.value;
    }

    /**
     * Xử lý khi thay đổi ngôn ngữ
     */
    handleLanguageChange() {
        this.updateVoicesList();
        // Xóa các audio đã tạo khi thay đổi ngôn ngữ
        this.audioFiles = [];
        this.audioContainer.style.display = 'none';
        this.audioContainer.innerHTML = '';
    }

    /**
     * Hiển thị/ẩn bảng cài đặt
     */
    toggleSettings() {
        const isVisible = this.settingsPanel.style.display !== 'none';
        this.settingsPanel.style.display = isVisible ? 'none' : 'block';

        // Đóng audio container khi mở settings và ngược lại
        if (!isVisible) {
            this.audioContainer.style.display = 'none';
        }
    }

    /**
     * Xử lý sự kiện khi click vào nút đọc tài liệu
     */
    handleTTSButtonClick() {
        if (this.audioFiles.length > 0) {
            // Nếu đã có audio, hiển thị/ẩn audio player
            this.toggleAudioContainer();
        } else {
            // Nếu chưa có audio, gọi API để chuyển đổi
            this.convertToSpeech();
        }
    }

    /**
     * Hiển thị/ẩn container audio
     */
    toggleAudioContainer() {
        const isVisible = this.audioContainer.style.display !== 'none';
        this.audioContainer.style.display = isVisible ? 'none' : 'block';

        // Dừng tất cả các audio nếu đang ẩn
        if (isVisible) {
            this.stopAllAudio();
        }
    }

    /**
     * Dừng tất cả các audio đang phát
     */
    stopAllAudio() {
        this.audioPlayers.forEach(player => {
            player.pause();
            player.currentTime = 0;
        });
        this.isPlaying = false;
        this.currentPlayerIndex = 0;

        if (this.playAllButton) {
            this.playAllButton.innerHTML = '<i class="fas fa-play"></i> Phát từ đầu';
        }

        // Xóa trạng thái active
        this.audioPlayers.forEach((_, i) => {
            const card = this.audioPlayers[i].closest('.tts-audio-part');
            if (card) {
                card.classList.remove('active-audio');
            }
        });
    }

    /**
     * Gọi API để chuyển đổi tài liệu thành giọng nói
     */
    async convertToSpeech() {
        try {
            // Lấy các tùy chọn giọng nói hiện tại
            this.options.languageCode = this.languageSelect.value;
            this.options.voiceName = this.voiceSelect.value;

            // Hiển thị loading
            this.loadingContainer.style.display = 'block';
            this.errorContainer.style.display = 'none';
            this.settingsPanel.style.display = 'none';
            this.ttsButton.disabled = true;
            this.convertButton.disabled = true;
            this.convertButton.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span> Đang xử lý...';

            // Gọi API
            const response = await fetch(`/api/TextToSpeech/document/${this.documentId}?languageCode=${this.options.languageCode}&voiceName=${this.options.voiceName}`);

            if (!response.ok) {
                throw new Error(`Lỗi: ${response.status} ${response.statusText}`);
            }

            const data = await response.json();

            if (data.success) {
                // Đảm bảo thứ tự các file âm thanh từ đầu đến cuối
                this.audioFiles = data.audioFiles;
                console.log("Tệp âm thanh nhận được:", this.audioFiles);
                this.createAudioPlayers();

                // Cập nhật nút chính để hiển thị trạng thái đã chuyển đổi
                this.ttsButton.innerHTML = `<i class="fas fa-volume-up me-2"></i> Nghe tài liệu`;
                this.ttsButton.classList.replace('btn-outline-primary', 'btn-success');

                // Hiển thị kết quả
                this.toggleAudioContainer();
            } else {
                throw new Error(data.message || 'Có lỗi xảy ra khi chuyển đổi');
            }
        } catch (error) {
            console.error('TTS Error:', error);
            this.errorContainer.textContent = `${this.options.errorText}: ${error.message}`;
            this.errorContainer.style.display = 'block';
        } finally {
            this.loadingContainer.style.display = 'none';
            this.ttsButton.disabled = false;
            this.convertButton.disabled = false;
            this.convertButton.innerHTML = '<i class="fas fa-play me-2"></i> Chuyển đổi và nghe';
        }
    }

    /**
     * Tạo các audio player từ danh sách file âm thanh
     */
    createAudioPlayers() {
        if (!this.audioFiles || this.audioFiles.length === 0) {
            return;
        }

        // Xóa các audio player cũ
        this.audioContainer.innerHTML = '';
        this.audioPlayers = [];

        // Thêm tiêu đề
        const titleDiv = document.createElement('div');
        titleDiv.className = 'tts-audio-title';
        titleDiv.innerHTML = `
            <i class="fas fa-podcast text-primary fs-4"></i>
            <div>
                <h6 class="mb-0">Nghe tài liệu (từ đầu đến cuối)</h6>
                <small class="text-muted">${this.audioFiles.length} phần âm thanh</small>
            </div>
        `;
        this.audioContainer.appendChild(titleDiv);

        // Thêm thanh tiến trình
        const progressContainer = document.createElement('div');
        progressContainer.className = 'tts-progress-container';
        progressContainer.innerHTML = '<div class="tts-progress-bar" style="width: 0%"></div>';
        this.audioContainer.appendChild(progressContainer);
        this.progressBar = progressContainer.querySelector('.tts-progress-bar');

        // Tạo bảng điều khiển phát tất cả
        const controlsWrapper = document.createElement('div');
        controlsWrapper.className = 'tts-controls-panel';

        // Nút phát/dừng tất cả
        this.playAllButton = document.createElement('button');
        this.playAllButton.type = 'button';
        this.playAllButton.className = 'btn btn-success';
        this.playAllButton.innerHTML = '<i class="fas fa-play"></i> Phát từ đầu';
        this.playAllButton.addEventListener('click', () => this.togglePlayAll());

        // Nút dừng
        const stopButton = document.createElement('button');
        stopButton.type = 'button';
        stopButton.className = 'btn btn-danger';
        stopButton.innerHTML = '<i class="fas fa-stop"></i> Dừng';
        stopButton.addEventListener('click', () => this.stopAllAudio());

        // Nút phát từ đầu
        const playFromStartButton = document.createElement('button');
        playFromStartButton.type = 'button';
        playFromStartButton.className = 'btn btn-primary';
        playFromStartButton.innerHTML = '<i class="fas fa-redo"></i> Bắt đầu lại';
        playFromStartButton.addEventListener('click', () => this.playFromBeginning());

        // Hiển thị thời gian
        this.timeDisplay = document.createElement('div');
        this.timeDisplay.className = 'tts-time-display';
        this.timeDisplay.textContent = '00:00 / 00:00';

        // Thêm vào wrapper
        controlsWrapper.appendChild(this.playAllButton);
        controlsWrapper.appendChild(stopButton);
        controlsWrapper.appendChild(playFromStartButton);
        controlsWrapper.appendChild(this.timeDisplay);
        this.audioContainer.appendChild(controlsWrapper);

        // Wrapper cho các audio part
        const audioPartsWrapper = document.createElement('div');
        audioPartsWrapper.className = 'tts-audio-parts';
        this.audioContainer.appendChild(audioPartsWrapper);

        // Tạo các audio player mới
        this.audioFiles.forEach((audioFile, index) => {
            const audioWrapper = document.createElement('div');
            audioWrapper.className = 'tts-audio-part card';

            // Tiêu đề
            const titleElement = document.createElement('div');
            titleElement.className = 'card-header';
            titleElement.innerHTML = `
                <div class="audio-part-header">
                    <div class="audio-part-title">
                        <i class="fas fa-file-audio text-primary"></i>
                        <div>
                            <span>Phần ${index + 1}</span>
                            <span class="audio-duration ms-2" id="duration-${index}">--:--</span>
                        </div>
                    </div>
                    <div class="audio-part-actions">
                        <button class="audio-play-btn" title="Phát phần này">
                            <i class="fas fa-play-circle"></i>
                        </button>
                        <a href="${audioFile}" download="audio_part_${index + 1}.mp3" class="btn-link" title="Tải xuống">
                            <i class="fas fa-download"></i>
                        </a>
                    </div>
                </div>
            `;

            // Audio player
            const audioContainer = document.createElement('div');
            audioContainer.className = 'card-body';

            const audioElement = document.createElement('audio');
            audioElement.controls = true;
            audioElement.className = 'w-100';
            audioElement.preload = 'metadata';

            const sourceElement = document.createElement('source');
            sourceElement.src = audioFile;
            sourceElement.type = 'audio/mpeg';

            // Thêm sự kiện để phát các phần liên tiếp
            audioElement.addEventListener('ended', () => {
                this.playNextAudio(index);
            });

            // Cập nhật tiến trình phát
            audioElement.addEventListener('timeupdate', () => {
                if (index === this.currentPlayerIndex && this.isPlaying) {
                    this.updateProgress();
                }
            });

            // Hiển thị thời lượng khi đã tải metadata
            audioElement.addEventListener('loadedmetadata', () => {
                const durationElement = titleElement.querySelector(`#duration-${index}`);
                if (durationElement) {
                    durationElement.textContent = this.formatTime(audioElement.duration);
                }
            });

            // Thêm sự kiện cho nút phát
            const playButton = titleElement.querySelector('.audio-play-btn');
            playButton.addEventListener('click', () => {
                if (this.currentPlayerIndex === index && this.isPlaying) {
                    this.pauseAllAudio();
                } else {
                    this.stopAllAudio();
                    this.currentPlayerIndex = index;
                    this.playAllAudio();
                }
            });

            audioElement.appendChild(sourceElement);
            audioContainer.appendChild(audioElement);

            audioWrapper.appendChild(titleElement);
            audioWrapper.appendChild(audioContainer);
            audioPartsWrapper.appendChild(audioWrapper);

            this.audioPlayers.push(audioElement);
        });

        // Hiển thị container audio
        this.audioContainer.style.display = 'block';

        // Mặc định phát từ phần đầu tiên
        this.currentPlayerIndex = 0;

        // Tự động nhấp mạnh đoạn đầu tiên
        if (this.audioPlayers.length > 0) {
            const firstAudioCard = this.audioPlayers[0].closest('.tts-audio-part');
            if (firstAudioCard) {
                firstAudioCard.classList.add('active-audio');
                setTimeout(() => {
                    firstAudioCard.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }, 500);
            }
        }
    }

    /**
     * Cập nhật thanh tiến trình và thời gian phát
     */
    updateProgress() {
        const currentPlayer = this.audioPlayers[this.currentPlayerIndex];
        if (!currentPlayer) return;

        // Tính tổng thời gian của tất cả audio
        let totalDuration = 0;
        this.audioPlayers.forEach(player => totalDuration += player.duration || 0);

        // Tính thời gian đã phát
        let playedTime = 0;
        for (let i = 0; i < this.currentPlayerIndex; i++) {
            playedTime += this.audioPlayers[i].duration || 0;
        }
        playedTime += currentPlayer.currentTime || 0;

        // Cập nhật thanh tiến trình
        const progressPercent = (playedTime / totalDuration) * 100;
        if (this.progressBar) {
            this.progressBar.style.width = `${progressPercent}%`;
        }

        // Cập nhật hiển thị thời gian
        if (this.timeDisplay) {
            this.timeDisplay.textContent = `${this.formatTime(playedTime)} / ${this.formatTime(totalDuration)}`;
        }

        // Cập nhật icon cho nút play phần đang phát
        this.audioPlayers.forEach((_, i) => {
            const card = this.audioPlayers[i].closest('.tts-audio-part');
            if (!card) return;

            const playBtn = card.querySelector('.audio-play-btn i');
            if (!playBtn) return;

            if (i === this.currentPlayerIndex && this.isPlaying) {
                playBtn.className = 'fas fa-pause-circle';
            } else {
                playBtn.className = 'fas fa-play-circle';
            }
        });
    }

    /**
     * Định dạng thời gian từ giây sang mm:ss
     */
    formatTime(seconds) {
        if (isNaN(seconds) || !isFinite(seconds)) return '00:00';
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }

    /**
     * Chuyển đổi trạng thái phát tất cả
     */
    togglePlayAll() {
        if (this.isPlaying) {
            this.pauseAllAudio();
            this.playAllButton.innerHTML = '<i class="fas fa-play"></i> Phát từ đầu';
        } else {
            // Reset vị trí phát về đầu nếu không đang phát
            if (this.currentPlayerIndex >= this.audioPlayers.length ||
                (this.audioPlayers[this.currentPlayerIndex] &&
                    this.audioPlayers[this.currentPlayerIndex].currentTime >= this.audioPlayers[this.currentPlayerIndex].duration - 0.1)) {
                this.currentPlayerIndex = 0;
            }
            this.playAllAudio();
            this.playAllButton.innerHTML = '<i class="fas fa-pause"></i> Tạm dừng';
        }
    }

    /**
     * Tạm dừng tất cả audio
     */
    pauseAllAudio() {
        if (this.currentPlayerIndex < this.audioPlayers.length) {
            this.audioPlayers[this.currentPlayerIndex].pause();
        }
        this.isPlaying = false;

        // Cập nhật icon cho nút ở phần đang phát
        const currentCard = this.audioPlayers[this.currentPlayerIndex]?.closest('.tts-audio-part');
        if (currentCard) {
            const playBtn = currentCard.querySelector('.audio-play-btn i');
            if (playBtn) {
                playBtn.className = 'fas fa-play-circle';
            }
        }
    }

    /**
     * Phát tất cả các audio liên tiếp
     */
    playAllAudio() {
        if (this.audioPlayers.length === 0) return;

        this.isPlaying = true;
        this.playAudioAtIndex(this.currentPlayerIndex);
    }

    /**
     * Phát audio tại chỉ mục cụ thể
     */
    playAudioAtIndex(index) {
        if (index >= this.audioPlayers.length) {
            this.isPlaying = false;
            this.currentPlayerIndex = 0;
            this.playAllButton.innerHTML = '<i class="fas fa-play"></i> Phát từ đầu';
            return;
        }

        // Đảm bảo tất cả các audio khác đều dừng
        this.audioPlayers.forEach((player, i) => {
            if (i !== index) {
                player.pause();
                player.currentTime = 0;
            }
        });

        // Phát audio hiện tại
        const player = this.audioPlayers[index];
        this.currentPlayerIndex = index;

        // Cuộn đến audio đang phát
        const audioElement = this.audioPlayers[index];
        const audioCardElement = audioElement.closest('.tts-audio-part');
        if (audioCardElement) {
            audioCardElement.scrollIntoView({ behavior: 'smooth', block: 'nearest' });

            // Thêm lớp active cho dễ nhận biết đoạn đang phát
            this.audioPlayers.forEach((_, i) => {
                const card = this.audioPlayers[i].closest('.tts-audio-part');
                if (card) {
                    if (i === index) {
                        card.classList.add('active-audio');
                    } else {
                        card.classList.remove('active-audio');
                    }
                }
            });
        }

        // Phát audio
        player.play().catch(error => {
            console.error('Lỗi khi phát audio:', error);
            this.playNextAudio(index);
        });

        // Cập nhật tiến trình ngay lập tức
        this.updateProgress();
    }

    /**
     * Phát audio tiếp theo sau khi một audio kết thúc
     */
    playNextAudio(currentIndex) {
        if (this.isPlaying) {
            this.playAudioAtIndex(currentIndex + 1);
        }
    }

    /**
     * Bắt đầu phát từ đầu
     */
    playFromBeginning() {
        this.currentPlayerIndex = 0;
        this.stopAllAudio();
        this.isPlaying = true;
        this.playAudioAtIndex(0);
        this.playAllButton.innerHTML = '<i class="fas fa-pause"></i> Tạm dừng';
    }
}

// Đăng ký component vào window để có thể sử dụng từ bất kỳ đâu
window.DocumentTTS = DocumentTTS; 