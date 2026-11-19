export default function EventModal({ onClose, children }) {
  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="detail modal" onClick={(e) => e.stopPropagation()}>
        <button type="button" className="modal-close" onClick={onClose}>
          &times;
        </button>
        {children}
      </div>
    </div>
  );
}