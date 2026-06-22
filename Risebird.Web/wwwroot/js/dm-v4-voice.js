/**
 * DM Approval V4 — voice brief recording, Whisper transcription, editable transcript, seed rubric/questions.
 */
(function ($) {
    'use strict';

    var RUBRIC_REGEN_ERROR = "Couldn't regenerate the rubric — please try again or set it manually.";

    function getIrfId() {
        var $root = $('#divIRFaudioRecording');
        var id = $root.data('irf-id') || $root.attr('data-irf-id');
        if (id) return parseInt(id, 10);
        var m = /irfid=(\d+)/i.exec(window.location.search);
        return m ? parseInt(m[1], 10) : 0;
    }

    function formatTranscriptDate(iso) {
        if (!iso) return '';
        try {
            var d = new Date(iso);
            if (isNaN(d.getTime())) return '';
            return d.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
        } catch (e) {
            return '';
        }
    }

    function setTranscriptUiState(state) {
        var $wrap = $('#dmV4VoiceTranscriptWrap');
        $wrap.removeClass('is-transcribing is-saving is-regenerating is-empty');
        if (state) $wrap.addClass(state);
    }

    function clearRubricRegenErrors() {
        $('#dmV4VoiceRubricError, #dmV4ScoreRubricRegenError').hide().text('');
    }

    function showRubricRegenError(message) {
        var msg = message || RUBRIC_REGEN_ERROR;
        $('#dmV4VoiceRubricError, #dmV4ScoreRubricRegenError').text(msg).show();
    }

    function applyTranscriptToHero(transcript, updatedOn) {
        var $ta = $('#dmV4VoiceTranscriptInput');
        var $meta = $('#dmV4VoiceTranscriptMeta');
        var $empty = $('#dmV4VoiceTranscriptEmpty');
        var hasText = transcript && String(transcript).trim().length > 0;

        if ($ta.length) {
            $ta.val(hasText ? transcript : '');
            $ta.prop('readonly', false);
        }
        if ($empty.length) $empty.toggle(!hasText);
        if ($meta.length) {
            if (hasText && updatedOn) {
                $meta.text('Last saved ' + formatTranscriptDate(updatedOn)).show();
            } else if (hasText) {
                $meta.text('Edit and save to set the canonical brief for AI generation.').show();
            } else {
                $meta.hide().text('');
            }
        }
        setTranscriptUiState(hasText ? '' : 'is-empty');

        $('#dmV4GenerateRubricFromVoiceBtn, #dmV4GenerateQuestionsFromVoiceBtn').prop('disabled', !hasText);
    }

    function showRubricVoiceResult(rubricRes) {
        var msg = (rubricRes && rubricRes.message) || 'Score rubric updated from voice brief.';
        var changed = rubricRes && rubricRes.changed === true;
        var icon = changed ? 'success' : 'info';
        var title = changed ? 'Rubric revised' : 'Rubric unchanged';

        if (changed && rubricRes.revisedSkills && typeof window.dmV4ApplyVoiceRevisedRubric === 'function') {
            var applied = window.dmV4ApplyVoiceRevisedRubric(rubricRes.revisedSkills);
            if (!applied) {
                msg += ' Review the score rubric table and save manually if needed.';
            }
        }

        if (typeof swal !== 'undefined') {
            swal.fire(title, msg, icon);
        } else {
            alert(title + ': ' + msg);
        }
    }

    function applyTranscriptToModal(transcript) {
        var $ta = $('#dmV4ModalTranscriptInput');
        var $wrap = $('#dmV4ModalTranscriptSection');
        if (!$ta.length) return;
        var hasText = transcript && String(transcript).trim().length > 0;
        $ta.val(hasText ? transcript : '');
        $wrap.toggle(hasText || $('#playUploadedAudioTwo').not('.dm-v4-voice-modal__saved--empty').length > 0);
    }

    function buildWaveformBarsHtml(count) {
        var html = '';
        for (var i = 0; i < count; i++) {
            var h = 6 + Math.floor(Math.abs(Math.sin(i * 0.5) * Math.sin(i * 0.18)) * 28) + (i % 3 === 0 ? 4 : 0);
            html += '<span class="dm-v4-voice-waveform__bar" style="height:' + h + 'px" data-bar="' + i + '"></span>';
        }
        return html;
    }

    function ensureVoiceWaveformBars() {
        var $wave = $('#dmV4VoiceWaveform');
        if (!$wave.length || $wave.find('.dm-v4-voice-waveform__bar').length) return;
        $wave.html(buildWaveformBarsHtml(64));
    }

    /** Show hero waveform player immediately after upload (new channels have no server-rendered audio UI). */
    function applyVoiceRecordingToHero(audioUrl) {
        if (!audioUrl || !String(audioUrl).trim()) return;
        if (!$('#dmV4VoiceHero').length) return;

        audioUrl = String(audioUrl).trim();
        var audio = document.getElementById('dmV4VoiceAudio');
        if (!audio) return;

        if (audio.getAttribute('src') !== audioUrl) {
            audio.setAttribute('src', audioUrl);
            try { audio.load(); } catch (e) { /* ignore */ }
        }

        ensureVoiceWaveformBars();
        $('#dmV4VoiceRecordingEmpty').prop('hidden', true);
        $('#dmV4VoiceWaveRow').prop('hidden', false);
        $('#dmV4VoiceTime').prop('hidden', false).text('0:00 / —');
        $('#dmV4VoiceRecordedBadge').prop('hidden', false).text('RECORDED');
        $('#dmV4RetranscribeBtn').prop('hidden', false);
        $('#dmV4VoiceRecordBtn').prop('hidden', true);
        $('#dmV4VoiceRerecordBtn').prop('hidden', false);
        $('#dmV4VoiceActionsSpacer').prop('hidden', false);
        $('.dm-v4-voice-preview-grid').removeClass('dm-v4-voice-preview-grid--empty');

        if (typeof window.dmV4InitVoiceHero === 'function') {
            window.dmV4InitVoiceHero();
        }
    }

    window.dmV4ApplyVoiceRecordingToHero = applyVoiceRecordingToHero;

    /**
     * Calls /IRF/GenerateRubricFromVoice (reads saved transcript from DB by irfId).
     * Returns a jQuery promise.
     */
    function callGenerateRubricFromVoice(irfId, $btn) {
        clearRubricRegenErrors();

        if ($btn && $btn.length) {
            $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Regenerating rubric…');
        }

        return $.ajax({
            type: 'POST',
            url: '/IRF/GenerateRubricFromVoice',
            data: { irfId: irfId },
            timeout: 300000
        }).then(function (rubricRes) {
            if (rubricRes && rubricRes.success) {
                showRubricVoiceResult(rubricRes);
                return rubricRes;
            }
            var msg = (rubricRes && rubricRes.message) || RUBRIC_REGEN_ERROR;
            showRubricRegenError(msg);
            console.error('[dm-v4-voice] GenerateRubricFromVoice returned failure:', { irfId: irfId, response: rubricRes });
            return $.Deferred().reject(rubricRes || { message: msg }).promise();
        }, function (xhr, textStatus, err) {
            showRubricRegenError(RUBRIC_REGEN_ERROR);
            console.error('[dm-v4-voice] GenerateRubricFromVoice request failed:', {
                irfId: irfId,
                textStatus: textStatus,
                error: err,
                status: xhr && xhr.status,
                responseText: xhr && xhr.responseText
            });
            return $.Deferred().reject({ message: RUBRIC_REGEN_ERROR, xhr: xhr }).promise();
        });
    }

    window.dmV4RefreshVoiceBrief = function (irfId) {
        irfId = irfId || getIrfId();
        if (!irfId) return $.Deferred().reject().promise();

        return $.ajax({
            type: 'POST',
            url: '/IRF/FetchVoiceInstructionBrief',
            data: { irfid: irfId }
        }).done(function (res) {
            if (!res || !res.success) return;
            if (res.audioUrl) {
                if (typeof dmV4SetVoiceModalSavedLink === 'function') {
                    dmV4SetVoiceModalSavedLink(res.audioUrl);
                }
                applyVoiceRecordingToHero(res.audioUrl);
            }
            applyTranscriptToHero(res.transcript, res.transcriptUpdatedOn);
            applyTranscriptToModal(res.transcript);
        });
    };

    window.dmV4TranscribeVoiceInstruction = function (irfId) {
        irfId = irfId || getIrfId();
        if (!irfId) return $.Deferred().reject().promise();

        setTranscriptUiState('is-transcribing');
        if (typeof dmV4SetRecordingStatus === 'function') {
            dmV4SetRecordingStatus('Transcribing your recording with AI…', 'uploading');
        }

        return $.ajax({
            type: 'POST',
            url: '/IRF/TranscribeVoiceInstruction',
            data: { irfId: irfId, isMultiLingual: false },
            timeout: 180000
        }).done(function (res) {
            if (res && res.success) {
                applyTranscriptToHero(res.transcript, res.transcriptUpdatedOn);
                applyTranscriptToModal(res.transcript);
                if (typeof dmV4SetRecordingStatus === 'function') {
                    dmV4SetRecordingStatus('Transcript ready — review below, edit if needed, then save.', 'success');
                }
            } else {
                var msg = (res && res.message) ? res.message : 'Transcription failed.';
                if (typeof dmV4SetRecordingStatus === 'function') {
                    dmV4SetRecordingStatus(msg, 'active');
                }
                if (typeof swal !== 'undefined') swal.fire('Transcription', msg, 'warning');
            }
        }).fail(function (xhr, textStatus) {
            var msg = textStatus === 'timeout'
                ? 'Transcription timed out. Try again or save a shorter recording.'
                : 'Transcription request failed.';
            if (typeof dmV4SetRecordingStatus === 'function') {
                dmV4SetRecordingStatus(msg, 'active');
            }
            if (typeof swal !== 'undefined') swal.fire('Transcription', msg, 'error');
        }).always(function () {
            setTranscriptUiState($('#dmV4VoiceTranscriptInput').val().trim() ? '' : 'is-empty');
        });
    };

    window.dmV4SaveVoiceTranscript = function (irfId, transcript, $btn) {
        irfId = irfId || getIrfId();
        transcript = (transcript != null ? transcript : $('#dmV4VoiceTranscriptInput').val() || $('#dmV4ModalTranscriptInput').val() || '').trim();
        if (!irfId || !transcript) {
            if (typeof swal !== 'undefined') swal.fire('Save transcript', 'Enter transcript text before saving.', 'warning');
            return $.Deferred().reject().promise();
        }

        var origHtml = $btn && $btn.length ? $btn.html() : '';
        clearRubricRegenErrors();

        if ($btn && $btn.length) {
            $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Saving…');
        }
        setTranscriptUiState('is-saving');

        return $.ajax({
            type: 'POST',
            url: '/IRF/UpdateVoiceInstructionTranscript',
            data: { irfId: irfId, transcript: transcript, regenerateRubric: false }
        }).then(function (res) {
            if (!res || !res.success) {
                var msg = (res && res.message) ? res.message : 'Could not save transcript.';
                if (typeof swal !== 'undefined') swal.fire('Save failed', msg, 'error');
                return $.Deferred().reject(res || { message: msg }).promise();
            }

            applyTranscriptToHero(res.transcript, res.transcriptUpdatedOn);
            applyTranscriptToModal(res.transcript);

            if (typeof swal !== 'undefined') swal.fire('Saved', 'Voice brief transcript saved.', 'success');
            return null;
        }, function () {
            if (typeof swal !== 'undefined') swal.fire('Save failed', 'Server error while saving transcript.', 'error');
            return $.Deferred().reject().promise();
        }).always(function () {
            setTranscriptUiState($('#dmV4VoiceTranscriptInput').val().trim() ? '' : 'is-empty');
            if ($btn && $btn.length) {
                $btn.prop('disabled', false).html(origHtml);
            }
        });
    };

    function initVoiceBriefPage() {
        if (!$('#dmV4VoiceHero').length) return;

        var initialTranscript = ($('#dmV4VoiceTranscriptInput').val() || '').trim();
        var initialUpdated = $('#dmV4VoiceTranscriptMeta').data('updated-on');
        applyTranscriptToHero(initialTranscript, initialUpdated);

        $(document).off('click.dmV4SaveTx', '#dmV4SaveTranscriptBtn, #dmV4ModalSaveTranscriptBtn')
            .on('click.dmV4SaveTx', '#dmV4SaveTranscriptBtn, #dmV4ModalSaveTranscriptBtn', function () {
                dmV4SaveVoiceTranscript(getIrfId(), null, $(this));
            });

        $(document).off('click.dmV4Retranscribe', '#dmV4RetranscribeBtn')
            .on('click.dmV4Retranscribe', '#dmV4RetranscribeBtn', function () {
                dmV4TranscribeVoiceInstruction(getIrfId());
            });

        $(document).off('click.dmV4GenRubricVoice', '#dmV4GenerateRubricFromVoiceBtn')
            .on('click.dmV4GenRubricVoice', '#dmV4GenerateRubricFromVoiceBtn', function () {
                var $btn = $(this);
                var irfId = getIrfId();
                if (!irfId || $btn.prop('disabled')) return;
                var orig = $btn.html();
                callGenerateRubricFromVoice(irfId, $btn).always(function () {
                    $btn.prop('disabled', !$('#dmV4VoiceTranscriptInput').val().trim()).html(orig);
                });
            });

        $(document).off('click.dmV4GenQVoice', '#dmV4GenerateQuestionsFromVoiceBtn')
            .on('click.dmV4GenQVoice', '#dmV4GenerateQuestionsFromVoiceBtn', function () {
                var $btn = $(this);
                var irfId = getIrfId();
                if (!irfId || $btn.prop('disabled')) return;
                var orig = $btn.html();
                $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Generating…');
                $.ajax({
                    type: 'POST',
                    url: '/IRF/GenerateQuestionsFromRubric',
                    data: { irfId: irfId, useVoiceTranscript: true },
                    timeout: 300000
                }).done(function (res) {
                    if (res && res.success) {
                        if (typeof window.dmV4RefreshQuestions === 'function') window.dmV4RefreshQuestions();
                        if (typeof swal !== 'undefined') swal.fire('Questions generated', (res.count || 0) + ' question(s) created using voice brief + rubric.', 'success');
                    } else {
                        if (typeof swal !== 'undefined') swal.fire('Generation failed', (res && res.message) || 'Could not generate.', 'error');
                    }
                }).fail(function () {
                    if (typeof swal !== 'undefined') swal.fire('Error', 'Question generation request failed.', 'error');
                }).always(function () {
                    $btn.prop('disabled', !$('#dmV4VoiceTranscriptInput').val().trim()).html(orig);
                });
            });
    }

    $(initVoiceBriefPage);
})(jQuery);
