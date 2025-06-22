from operator import rshift
from api_interfaces import *

global logger
app_logger = get_disabled_logger()
logger = app_logger.get_logger(component_name="GDBTest")

def test_GenderDebiasResponseSerialize():
    response = get_json(GenderDebiasResponse("Doctor went home", "El medico se fue a casa"))
    print(response)
    assert response == '{"src_sentence": "Doctor went home", "tgt": "El medico se fue a casa"}'

def test_GenderDebiasSingleTokenRequest():
    response = get_json(GenderDebiasResponse("cook", "cocinera"))
    print(response)
    assert response == '{"src_sentence": "cook", "tgt": {"Feminine": "cocinera", "Masculine": "cocinero"}'

def test_GenderDebiasTwoTokenRequest():
    response = get_json(GenderDebiasResponse("thank you", "Gracias"))
    print(response)
    assert response == '{"src_sentence": "thank you", "tgt": {"Neutral": "Gracias"}'

def test_GenderDebiasErrorResponseSerialize():
    error_response = get_json(GenderDebiasErrorResponse(4001, "Missing target text"))
    assert error_response == '{"code": 4001, "message": "Missing target text"}'

def test_ValidGenderDebiasRequest():
    request = '{"source": { "language": "en", "text": "he is the best villain" }, "target": { "language": "es", "text": "es el mejor villano" }, "options": { "debug": true, "logInput": true, "traceId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse == None
    assert aml_request.src_lang == Language('en')
    assert aml_request.tgt_lang == Language('es')
    assert aml_request.src_text == "he is the best villain"
    assert aml_request.tgt_text == "es el mejor villano"
    assert aml_request.response_headers[client_traceid_response_header_name]== "abcdef"

def test_ValidGenderDebiasRequestWithUpperCaseJsonAttrib():
    request = '{"SOURCE": { "language": "en", "text": "he is the best villain" }, "target": { "language": "es", "text": "es el mejor villano" }, "options": { "debug": true, "logInput": true, "traceId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse.data == b'{"code": 40001, "message": "Missing source"}'

def test_InvalidJsonRequest():
    request = '{"source": { "text": "he is the best villain", "" }, "target": { "language": "es", "text": "es el mejor villano" }, "options": { "debug": true, "logInput": true, "traceId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse.data == b'{"code": 40000, "message": "Invalid json request"}'

def test_EmptySrcLangGenderDebiasRequest():
    request = '{"source": { "language": "", "text": "he is the best villain" }, "target": { "language": "es", "text": "es el mejor villano" }, "options": { "debug": true, "logInput": true, "traceId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse.data == b'{"code": 40009, "message": "Invalid source language"}'

def test_MissingSrcLangGenderDebiasRequest():
    request = '{"source": { "text": "he is the best villain" }, "target": { "language": "es", "text": "es el mejor villano" }, "options": { "debug": true, "logInput": true, "traceId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse.data == b'{"code": 40003, "message": "Missing source language"}'


def test_InvalidSrcLangGenderDebiasRequest():
    request = '{"source": { "language": "abc", "text": "he is the best villain" }, "target": { "language": "es", "text": "es el mejor villano" }, "options": { "debug": true, "logInput": true, "traceId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse.data == b'{"code": 40009, "message": "Invalid source language"}'

def test_UnsupportedSrcLangGenderDebiasRequest():
    request = '{"source": { "language": "es", "text": "he is the best villain" }, "target": { "language": "fr", "text": "es el mejor villano" }, "options": { "debug": true, "logInput": true, "traceId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse.data == b'{"code": 40010, "message": "Unsupported source language"}'

def test_MissingTgtLangGenderDebiasRequest():
    request = '{"source": { "language": "en", "text": "he is the best villain" }, "target": { "text": "es el mejor villano" }, "options": { "debug": true, "logInput": true, "traceId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse.data == b'{"code": 40006, "message": "Missing target language"}'

def test_InvalidTgtLangGenderDebiasRequest():
    request = '{"source": { "language": "en", "text": "he is the best villain" }, "target": { "language": "abc", "text": "es el mejor villano" }, "options": { "debug": true, "logInput": true, "traceId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse.data == b'{"code": 40011, "message": "Invalid target language"}'

def test_UnsupportedTgtLangGenderDebiasRequest():
    request = '{"source": { "language": "en", "text": "he is the best villain" }, "target": { "language": "en", "text": "es el mejor villano" }, "options": { "debug": true, "logInput": true, "traceId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse.data == b'{"code": 40012, "message": "Unsupported target language"}'


def test_EmptySrcTextGenderDebiasRequest():
    request = '{"source": { "language": "en", "text": "" }, "target": { "language": "es", "text": "es el mejor villano" }, "options": { "debug": true, "logInput": true, "traceId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse.data == b'{"code": 40007, "message": "Source text empty"}'

def test_EmptyTgtTextGenderDebiasRequest():
    request = '{"source": { "language": "en", "text": "How are you?" }, "target": { "language": "es", "text": "" }, "options": { "debug": true, "logInput": true, "correlationId": "abcdef" } }'
    aml_request, errorResponse = validate_request(request, logger)
    assert errorResponse.data == b'{"code": 40008, "message": "Target text empty"}'


def test_sentfix_normalization():
    from source_based_sentfix import get_case_signature, CaseSignature, apply_case_signature, normalize_for_sentfix_match, add_end_punc_to_target
    assert get_case_signature("3 BIG BOTTLES OF SHAMPOO!") == CaseSignature.AllCaps, "assertion failure with get_case_signature"
    assert get_case_signature("3 Big BOTTLES OF SHAMPOO!") == CaseSignature.InitCaps, "assertion failure with get_case_signature"
    assert get_case_signature("3 bIG BOTTLES OF SHAMPOO!") == CaseSignature.Other, "assertion failure with get_case_signature"
    assert get_case_signature("A") == CaseSignature.InitCaps, "assertion failure with get_case_signature"
    assert get_case_signature("a") == CaseSignature.Other, "assertion failure with get_case_signature"

    assert apply_case_signature('"big bottles of shampoo"', CaseSignature.AllCaps) == '"BIG BOTTLES OF SHAMPOO"', "assertion failure with apply_case_signature"
    assert apply_case_signature('"big bottles of shampoo"', CaseSignature.InitCaps) == '"Big bottles of shampoo"', "assertion failure with apply_case_signature"
    assert apply_case_signature('"big BOTTLES of Shampoo"', CaseSignature.Other) == '"big BOTTLES of Shampoo"', "assertion failure with apply_case_signature"
    assert apply_case_signature('b', CaseSignature.InitCaps) == 'B', "assertion failure with apply_case_signature"
    assert apply_case_signature('3 b', CaseSignature.InitCaps) == '3 B', "assertion failure with apply_case_signature"

    assert normalize_for_sentfix_match('A test.') == ('a test', '.'), "assertion failure with normalize_for_sentfix_match"
    assert normalize_for_sentfix_match('A. test!') == ('a. test', '!'), "assertion failure with normalize_for_sentfix_match"

    assert add_end_punc_to_target(Language.French, "A test", "!") == "A test !", "assertion failure with add_end_punc_to_target"
    assert add_end_punc_to_target(Language.Spanish, "A test", "!") == "¡A test!", "assertion failure with add_end_punc_to_target"
    assert add_end_punc_to_target(Language.Italian, "A test", "!") == "A test!", "assertion failure with add_end_punc_to_target"
    assert add_end_punc_to_target(Language.Italian, "A test", None) == "A test", "assertion failure with add_end_punc_to_target"
    assert add_end_punc_to_target(Language.French, "A test", ".") == "A test.", "assertion failure with add_end_punc_to_target"
    assert add_end_punc_to_target(Language.Spanish, "A test", ".") == "A test.", "assertion failure with add_end_punc_to_target"
    assert add_end_punc_to_target(Language.Italian, "A test", ".") == "A test.", "assertion failure with add_end_punc_to_target"


def test_phrase_exclusion():
    from phrase_exclusion import build_phrase_exclusion_trie, check_phrase_exclusion_with_boundaries
    # Manually defining token boundaries -- Don't want to have to interact with model files or downloading in unit test

    phrase_exclusion_trie = build_phrase_exclusion_trie(['apple pie'])
    test_sentence1 = "I'm a fan of apple pie."
    boundaries1 = set([0, 1, 2, 3, 4, 5, 6, 9, 10, 12, 13, 18, 19, 22, 23])

    assert check_phrase_exclusion_with_boundaries(test_sentence1, boundaries1, phrase_exclusion_trie), \
        f"Expected phrase exclusion=True on {test_sentence1}"

    test_sentence2 = "I like abcapple pie"
    boundaries2 = set([0, 1, 2, 6, 7, 15, 16, 19])

    assert not check_phrase_exclusion_with_boundaries(test_sentence2, boundaries2, phrase_exclusion_trie), \
        f"Expected phrase exclusion=False on {test_sentence2}"

    test_sentence3 = "I like apple pieabc"
    boundaries3 = set([0, 1, 2, 6, 7, 12, 13, 19])

    assert not check_phrase_exclusion_with_boundaries(test_sentence3, boundaries3, phrase_exclusion_trie), \
        f"Expected phrase exclusion=False on {test_sentence3}"

    test_sentence4 = "I like applepie"
    boundaries4 = set([0, 1, 2, 6, 7, 15])

    assert not check_phrase_exclusion_with_boundaries(test_sentence4, boundaries4, phrase_exclusion_trie), \
        f"Expected phrase exclusion=False on {test_sentence4}"

    # Check case invariance and beginning punctuation 
    test_sentence5 = "(Apple Pie)"
    boundaries5 = set([0, 1, 6, 7, 10, 11])

    assert check_phrase_exclusion_with_boundaries(test_sentence5, boundaries5, phrase_exclusion_trie), \
        f"Expected phrase exclusion=True on {test_sentence5}"

def test_normalize_space():
    from gender_bias_utils import normalize_space
    assert normalize_space("\ud808\udf45 Test\0test\xa0\x10\xa0Test \n\t\r Test \xa0") == "Testtest Test Test"
