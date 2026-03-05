/**
 * Admin Image Manager - Handles image upload, preview, and management for admin edit pages
 */
class AdminImageManager {
    constructor(options) {
        this.fileInputId = options.fileInputId || 'ImageFiles';
        this.containerId = options.containerId || 'all-images-container';
        this.noImagesMessageId = options.noImagesMessageId || 'no-images-message';
        this.deleteHandler = options.deleteHandler; // URL for delete endpoint
        this.deleteParams = options.deleteParams; // Function that returns params object
        
        this.fileInput = document.getElementById(this.fileInputId);
        this.container = document.getElementById(this.containerId);
        this.noImagesMessage = document.getElementById(this.noImagesMessageId);
        
        this.selectedFiles = [];
        this.newImageIndex = 0;
        
        this.init();
    }
    
    init() {
        this.initializeMainImageTracking();
        this.attachEventListeners();
    }
    
    initializeMainImageTracking() {
        const mainExistingImage = document.querySelector('[data-image-type="existing"] .badge.bg-primary');
        if (mainExistingImage) {
            const container = mainExistingImage.closest('[data-image-container]');
            const imagePath = container.getAttribute('data-image-container');
            document.getElementById('MainImagePath').value = imagePath;
            document.getElementById('MainImageType').value = 'existing';
        }
    }
    
    attachEventListeners() {
        if (this.fileInput) {
            this.fileInput.addEventListener('change', (e) => {
                if (e.target.files.length > 0) {
                    this.handleFileSelection(e.target.files);
                }
            });
        }
        
        document.addEventListener('click', (e) => this.handleClick(e));
    }
    
    handleFileSelection(files) {
        if (this.noImagesMessage) {
            this.noImagesMessage.style.display = 'none';
        }
        
        const hasExistingImages = document.querySelectorAll('[data-image-type="existing"]').length > 0;
        
        Array.from(files).forEach((file) => {
            if (file.type.startsWith('image/')) {
                const previewId = `new-image-${this.newImageIndex++}`;
                this.selectedFiles.push({ file: file, previewId: previewId });
                
                const reader = new FileReader();
                reader.onload = (e) => {
                    const isFirstImage = this.selectedFiles.length === 1 && !hasExistingImages && !this.hasAnyMainImage();
                    const imageCard = this.createImagePreviewCard(e.target.result, previewId, file.name, isFirstImage);
                    this.container.appendChild(imageCard);
                };
                reader.readAsDataURL(file);
            }
        });
        
        this.updateFileInput();
    }
    
    updateFileInput() {
        const dataTransfer = new DataTransfer();
        this.selectedFiles.forEach(item => {
            dataTransfer.items.add(item.file);
        });
        this.fileInput.files = dataTransfer.files;
    }
    
    hasAnyMainImage() {
        return document.querySelector('[data-image-container] .badge.bg-primary') !== null;
    }
    
    createImagePreviewCard(src, previewId, fileName, isMain = false) {
        const col = document.createElement('div');
        col.className = 'col-md-3 col-sm-4 col-6';
        col.setAttribute('data-image-container', previewId);
        col.setAttribute('data-image-type', 'preview');
        
        col.innerHTML = `
            <div class="card bg-secondary border-0 shadow-sm h-100" style="border: 2px dashed #28a745 !important;">
                <div class="position-relative">
                    <img src="${src}" class="card-img-top" 
                         style="height: 160px; object-fit: cover;" alt="Podgląd: ${fileName}" />
                    ${isMain ? '<span class="position-absolute top-0 start-0 badge bg-primary m-2"><i class="fas fa-star me-1"></i>Główne</span>' : ''}
                    <span class="position-absolute top-0 end-0 badge bg-info m-2">Nowe</span>
                </div>
                <div class="card-body p-2">
                    <div class="d-flex gap-1 flex-wrap">
                        ${!isMain ? `<button type="button" class="btn btn-sm btn-outline-primary set-main-preview-btn flex-fill" 
                                            data-image-path="${previewId}" title="Ustaw jako główne zdjęcie">
                                        <i class="fas fa-star me-1"></i>Ustaw jako główne
                                    </button>` : ''}
                        <button type="button" class="btn btn-sm btn-outline-danger remove-preview-btn flex-fill" 
                                data-image-path="${previewId}" title="Usuń z podglądu">
                            <i class="fas fa-trash me-1"></i>Usuń
                        </button>
                    </div>
                </div>
            </div>
        `;
        
        return col;
    }
    
    handleClick(event) {
        // Set main preview image
        const setMainPreviewButton = event.target.closest('.set-main-preview-btn');
        if (setMainPreviewButton) {
            event.preventDefault();
            const imagePath = setMainPreviewButton.getAttribute('data-image-path');
            this.setMainPreviewImage(imagePath);
            return;
        }
        
        // Remove preview image
        const removePreviewButton = event.target.closest('.remove-preview-btn');
        if (removePreviewButton) {
            event.preventDefault();
            const previewId = removePreviewButton.getAttribute('data-image-path');
            const container = removePreviewButton.closest('[data-image-container]');
            
            this.selectedFiles = this.selectedFiles.filter(item => item.previewId !== previewId);
            this.updateFileInput();
            container.remove();
            
            this.setMainImageAfterRemoval(container);
            
            const remainingImages = document.querySelectorAll('[data-image-container]');
            if (remainingImages.length === 0 && this.noImagesMessage) {
                this.noImagesMessage.style.display = 'block';
            }
            return;
        }
        
        // Delete existing image
        const deleteButton = event.target.closest('.delete-image-btn');
        if (deleteButton) {
            event.preventDefault();
            const imagePath = deleteButton.getAttribute('data-image-path');
            const params = this.deleteParams();
            
            if (!this.validateDeleteParams(params)) {
                this.showMessage('Błąd: Brak wymaganych parametrów', 'error');
                return;
            }
            
            if (confirm('Czy na pewno chcesz usunąć to zdjęcie?')) {
                this.deleteImage(imagePath, params, event.target);
            }
            return;
        }
        
        // Set main existing image
        if (event.target.matches('.set-main-btn')) {
            event.preventDefault();
            const imagePath = event.target.getAttribute('data-image-path');
            this.setMainExistingImage(imagePath);
        }
    }
    
    validateDeleteParams(params) {
        // Override in subclass if needed
        return Object.values(params).every(val => val != null && val !== '');
    }
    
    setMainImageAfterRemoval(removedContainer) {
        if (this.hasAnyMainImage()) {
            return;
        }
        
        const nextContainer = this.getNextMainContainer(removedContainer);
        if (!nextContainer) {
            document.getElementById('MainImagePath').value = '';
            document.getElementById('MainImageType').value = '';
            return;
        }
        
        const nextImagePath = nextContainer.getAttribute('data-image-container');
        const nextImageType = nextContainer.getAttribute('data-image-type');
        
        if (nextImageType === 'preview') {
            this.setMainPreviewImage(nextImagePath);
        } else {
            this.setMainExistingImage(nextImagePath);
        }
    }
    
    getNextMainContainer(removedContainer) {
        if (!removedContainer) {
            return document.querySelector('[data-image-container]');
        }
        
        const nextSibling = removedContainer.nextElementSibling;
        if (nextSibling?.matches('[data-image-container]')) {
            return nextSibling;
        }
        
        const previousSibling = removedContainer.previousElementSibling;
        if (previousSibling?.matches('[data-image-container]')) {
            return previousSibling;
        }
        
        return document.querySelector('[data-image-container]');
    }
    
    setMainExistingImage(imagePath) {
        this.clearAllMainBadges();
        this.showAllSetMainButtons();
        
        const container = document.querySelector(`[data-image-container="${imagePath}"]`);
        if (container) {
            const imageContainer = container.querySelector('.position-relative');
            imageContainer.insertAdjacentHTML('beforeend', '<span class="position-absolute top-0 start-0 badge bg-primary m-2"><i class="fas fa-star me-1"></i>Główne</span>');
            
            const setMainBtn = container.querySelector('.set-main-btn');
            if (setMainBtn) {
                setMainBtn.style.display = 'none';
            }
            
            document.getElementById('MainImagePath').value = imagePath;
            document.getElementById('MainImageType').value = 'existing';
        }
    }
    
    setMainPreviewImage(imagePath) {
        this.clearAllMainBadges();
        this.showAllSetMainButtons();
        
        const container = document.querySelector(`[data-image-container="${imagePath}"]`);
        if (container) {
            const imageContainer = container.querySelector('.position-relative');
            imageContainer.insertAdjacentHTML('beforeend', '<span class="position-absolute top-0 start-0 badge bg-primary m-2"><i class="fas fa-star me-1"></i>Główne</span>');
            
            const setMainBtn = container.querySelector('.set-main-preview-btn');
            if (setMainBtn) {
                setMainBtn.style.display = 'none';
            }
            
            document.getElementById('MainImagePath').value = imagePath;
            document.getElementById('MainImageType').value = 'preview';
        }
    }
    
    clearAllMainBadges() {
        document.querySelectorAll('[data-image-container] .badge.bg-primary').forEach(badge => badge.remove());
        document.querySelectorAll('[data-image-container] .badge.bg-success').forEach(badge => badge.remove());
    }
    
    showAllSetMainButtons() {
        document.querySelectorAll('.set-main-btn, .set-main-preview-btn').forEach(btn => {
            btn.style.display = 'inline-block';
        });
    }
    
    async deleteImage(imagePath, params, buttonElement) {
        try {
            const formData = new FormData();
            formData.append('imagePath', imagePath);
            Object.keys(params).forEach(key => {
                formData.append(key, params[key]);
            });
            
            const response = await fetch(this.deleteHandler, {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            });
            
            const result = await response.json();
            
            if (result.success) {
                const container = buttonElement.closest('[data-image-container]');
                const wasMainImage = container.querySelector('.badge.bg-primary') !== null;
                
                container.remove();
                
                if (wasMainImage) {
                    this.setMainImageAfterRemoval(null);
                }
                
                const remainingImages = document.querySelectorAll('[data-image-container]');
                if (remainingImages.length === 0 && this.noImagesMessage) {
                    this.noImagesMessage.style.display = 'block';
                }
                
                this.showMessage('Zdjęcie zostało usunięte', 'success');
            } else {
                this.showMessage(result.message || 'Błąd podczas usuwania zdjęcia', 'error');
            }
        } catch (error) {
            this.showMessage('Błąd podczas usuwania zdjęcia', 'error');
            console.error('Error deleting image:', error);
        }
    }
    
    showMessage(message, type) {
        const alertClass = type === 'success' ? 'alert-success' : 'alert-danger';
        const alertDiv = document.createElement('div');
        alertDiv.className = `alert ${alertClass} alert-dismissible fade show`;
        alertDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        const container = document.querySelector('.container');
        container.insertBefore(alertDiv, container.firstChild);
        
        setTimeout(() => {
            alertDiv.remove();
        }, 3000);
    }
}
